using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace GestionInteligente.Persistence
{
    public sealed class DatabaseConnection : IDisposable
    {
        private const int SqliteOk = 0;
        private const int SqliteRow = 100;
        private const int SqliteDone = 101;
        private const int SqliteNull = 5;
        private const int SqliteTransient = -1;

        private readonly object _lock = new object();
        private IntPtr _database;

        public DatabaseConnection(string databasePath)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor
                && Application.platform != RuntimePlatform.WindowsPlayer)
                throw new PlatformNotSupportedException("La conexión SQLite configurada requiere Windows.");

            var result = Native.sqlite3_open(Utf8(databasePath), out _database);
            if (result != SqliteOk)
            {
                var message = ErrorMessage;
                Dispose();
                throw new InvalidOperationException($"No se pudo abrir la base SQLite: {message}");
            }

            Execute("PRAGMA foreign_keys = ON");
            Execute("PRAGMA journal_mode = WAL");
            Execute("PRAGMA synchronous = NORMAL");
        }

        public void Execute(string sql, params object[] parameters)
        {
            lock (_lock)
            {
                using (var statement = Prepare(sql, parameters))
                {
                    var result = Native.sqlite3_step(statement.Handle);
                    if (result != SqliteDone && result != SqliteRow)
                        ThrowSqliteError(sql);
                }
            }
        }

        public void ExecuteScript(string sql)
        {
            lock (_lock)
            {
                IntPtr error = IntPtr.Zero;
                var result = Native.sqlite3_exec(_database, Utf8(sql), IntPtr.Zero, IntPtr.Zero, out error);
                if (result == SqliteOk) return;

                var message = error == IntPtr.Zero ? ErrorMessage : PtrToUtf8(error);
                if (error != IntPtr.Zero) Native.sqlite3_free(error);
                throw new InvalidOperationException($"Error SQLite ejecutando migración: {message}");
            }
        }

        public List<T> Query<T>(string sql, Func<DatabaseRow, T> map, params object[] parameters)
        {
            lock (_lock)
            {
                using (var statement = Prepare(sql, parameters))
                {
                    var rows = new List<T>();
                    int result;
                    while ((result = Native.sqlite3_step(statement.Handle)) == SqliteRow)
                        rows.Add(map(new DatabaseRow(statement.Handle)));

                    if (result != SqliteDone) ThrowSqliteError(sql);
                    return rows;
                }
            }
        }

        public T QueryFirstOrDefault<T>(string sql, Func<DatabaseRow, T> map, params object[] parameters)
        {
            var rows = Query(sql, map, parameters);
            return rows.Count == 0 ? default(T) : rows[0];
        }

        public int QueryInt(string sql, params object[] parameters)
        {
            return QueryFirstOrDefault(sql, row => row.GetInt32(0), parameters);
        }

        public void BeginTransaction() => Execute("BEGIN TRANSACTION");
        public void Commit() => Execute("COMMIT");
        public void Rollback() => Execute("ROLLBACK");

        public void Dispose()
        {
            lock (_lock)
            {
                if (_database == IntPtr.Zero) return;
                Native.sqlite3_close(_database);
                _database = IntPtr.Zero;
            }
        }

        private SqliteStatement Prepare(string sql, object[] parameters)
        {
            IntPtr handle;
            var result = Native.sqlite3_prepare_v2(_database, Utf8(sql), -1, out handle, IntPtr.Zero);
            if (result != SqliteOk) ThrowSqliteError(sql);

            var statement = new SqliteStatement(handle);
            try
            {
                for (var i = 0; i < parameters.Length; i++)
                    Bind(handle, i + 1, parameters[i]);
                return statement;
            }
            catch
            {
                statement.Dispose();
                throw;
            }
        }

        private void Bind(IntPtr statement, int index, object value)
        {
            int result;
            if (value == null)
            {
                result = Native.sqlite3_bind_null(statement, index);
            }
            else if (value is byte[] bytes)
            {
                result = Native.sqlite3_bind_blob(statement, index, bytes, bytes.Length, new IntPtr(SqliteTransient));
            }
            else if (value is bool boolean)
            {
                result = Native.sqlite3_bind_int(statement, index, boolean ? 1 : 0);
            }
            else if (value is int integer)
            {
                result = Native.sqlite3_bind_int(statement, index, integer);
            }
            else if (value is long longInteger)
            {
                result = Native.sqlite3_bind_int64(statement, index, longInteger);
            }
            else if (value is float || value is double || value is decimal)
            {
                result = Native.sqlite3_bind_double(statement, index, Convert.ToDouble(value));
            }
            else
            {
                var text = Utf8(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                result = Native.sqlite3_bind_text(statement, index, text, text.Length - 1, new IntPtr(SqliteTransient));
            }

            if (result != SqliteOk) ThrowSqliteError("bind");
        }

        private string ErrorMessage => PtrToUtf8(Native.sqlite3_errmsg(_database));

        private void ThrowSqliteError(string operation)
        {
            throw new InvalidOperationException($"Error SQLite ({operation}): {ErrorMessage}");
        }

        private static byte[] Utf8(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Array.Resize(ref bytes, bytes.Length + 1);
            return bytes;
        }

        private static string PtrToUtf8(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero) return string.Empty;
            var length = 0;
            while (Marshal.ReadByte(pointer, length) != 0) length++;
            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        private sealed class SqliteStatement : IDisposable
        {
            public IntPtr Handle { get; private set; }
            public SqliteStatement(IntPtr handle) => Handle = handle;
            public void Dispose()
            {
                if (Handle == IntPtr.Zero) return;
                Native.sqlite3_finalize(Handle);
                Handle = IntPtr.Zero;
            }
        }

        private static class Native
        {
            private const string Library = "winsqlite3";

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_open(byte[] filename, out IntPtr database);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_close(IntPtr database);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr sqlite3_errmsg(IntPtr database);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_prepare_v2(IntPtr database, byte[] sql, int byteCount, out IntPtr statement, IntPtr tail);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_step(IntPtr statement);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_finalize(IntPtr statement);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_bind_null(IntPtr statement, int index);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_bind_int(IntPtr statement, int index, int value);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_bind_int64(IntPtr statement, int index, long value);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_bind_double(IntPtr statement, int index, double value);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_bind_text(IntPtr statement, int index, byte[] value, int byteCount, IntPtr destructor);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_bind_blob(IntPtr statement, int index, byte[] value, int byteCount, IntPtr destructor);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_column_count(IntPtr statement);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr sqlite3_column_name(IntPtr statement, int index);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_column_type(IntPtr statement, int index);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr sqlite3_column_text(IntPtr statement, int index);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr sqlite3_column_blob(IntPtr statement, int index);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_column_bytes(IntPtr statement, int index);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern long sqlite3_column_int64(IntPtr statement, int index);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern double sqlite3_column_double(IntPtr statement, int index);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_exec(IntPtr database, byte[] sql, IntPtr callback, IntPtr argument, out IntPtr errorMessage);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void sqlite3_free(IntPtr pointer);
        }

        public sealed class DatabaseRow
        {
            private readonly IntPtr _statement;
            private readonly Dictionary<string, int> _columns;

            internal DatabaseRow(IntPtr statement)
            {
                _statement = statement;
                _columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var count = Native.sqlite3_column_count(statement);
                for (var i = 0; i < count; i++)
                    _columns[PtrToUtf8(Native.sqlite3_column_name(statement, i))] = i;
            }

            public string GetString(string column) => GetString(Index(column));
            public string GetString(int index)
            {
                if (Native.sqlite3_column_type(_statement, index) == SqliteNull) return null;
                return PtrToUtf8(Native.sqlite3_column_text(_statement, index));
            }

            public int GetInt32(string column) => GetInt32(Index(column));
            public int GetInt32(int index) => checked((int)Native.sqlite3_column_int64(_statement, index));
            public long GetInt64(string column) => Native.sqlite3_column_int64(_statement, Index(column));
            public double GetDouble(string column) => Native.sqlite3_column_double(_statement, Index(column));
            public double? GetNullableDouble(string column)
            {
                var index = Index(column);
                return Native.sqlite3_column_type(_statement, index) == SqliteNull
                    ? (double?)null
                    : Native.sqlite3_column_double(_statement, index);
            }
            public int? GetNullableInt32(string column)
            {
                var index = Index(column);
                return Native.sqlite3_column_type(_statement, index) == SqliteNull
                    ? (int?)null
                    : checked((int)Native.sqlite3_column_int64(_statement, index));
            }
            public byte[] GetBytes(string column)
            {
                var index = Index(column);
                if (Native.sqlite3_column_type(_statement, index) == SqliteNull) return null;
                var length = Native.sqlite3_column_bytes(_statement, index);
                var bytes = new byte[length];
                if (length > 0) Marshal.Copy(Native.sqlite3_column_blob(_statement, index), bytes, 0, length);
                return bytes;
            }

            private int Index(string column)
            {
                if (!_columns.TryGetValue(column, out var index))
                    throw new ArgumentException($"La columna '{column}' no existe en el resultado SQLite.", nameof(column));
                return index;
            }
        }
    }
}
