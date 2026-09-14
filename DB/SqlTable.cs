/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySqlConnector;
using TShockAPI.DB.Queries;

namespace TShockAPI.DB
{
	public class SqlTable
	{
		public List<SqlColumn> Columns { get; protected set; }
		public string Name { get; protected set; }

		public SqlTable(string name, params SqlColumn[] columns)
			: this(name, new List<SqlColumn>(columns))
		{
		}

		public SqlTable(string name, List<SqlColumn> columns)
		{
			Name = name;
			Columns = columns;
		}
	}

	public class SqlTableCreator
	{
		private IDbConnection database;
		private IQueryBuilder creator;

		public SqlTableCreator(IDbConnection db, IQueryBuilder provider)
		{
			database = db;
			creator = provider;
		}

		// Returns true if the table was created; false if it was not.
		public bool EnsureTableStructure(SqlTable table)
		{
			var columns = GetColumns(table);
			if (columns.Count > 0)
			{
				// Use OrdinalIgnoreCase to account for pgsql automatically lowering cases.
				if (!table.Columns.All(c => columns.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
				    || !columns.All(c => table.Columns.Any(c2 => c2.Name.Equals(c, StringComparison.OrdinalIgnoreCase))))
				{
					var from = new SqlTable(table.Name, columns.Select(s => new SqlColumn(s, MySqlDbType.String)).ToList());
					database.Query(creator.AlterTable(from, table));
				}
			}
			else
			{
				database.Query(creator.CreateTable(table));
				return true;
			}

			return false;
		}

		public List<string> GetColumns(SqlTable table)
		{
			List<string> ret = new();
			switch (database.GetSqlType())
			{
				case SqlType.Sqlite:
				{
					using QueryResult reader = database.QueryReader("PRAGMA table_info({0})".SFormat(table.Name));
					while (reader.Read())
					{
						ret.Add(reader.Get<string>("name"));
					}

					break;
				}
				case SqlType.Mysql:
				{
					using QueryResult reader =
						database.QueryReader("SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_NAME=@0 AND TABLE_SCHEMA=@1", table.Name, database.Database);

					while (reader.Read())
					{
						ret.Add(reader.Get<string>("COLUMN_NAME"));
					}

					break;
				}
				case SqlType.Postgres:
				{
					// HACK: Using "ilike" op to ignore case, due to weird case issues adapting for pgsql
					using QueryResult reader = database.QueryReader("SELECT column_name FROM information_schema.columns WHERE table_schema=current_schema() AND table_name ILIKE @0", table.Name);

					while (reader.Read())
					{
						ret.Add(reader.Get<string>("column_name"));
					}

					break;
				}
				default: throw new NotSupportedException();
			}

			return ret;
		}

		public void DeleteRow(string table, List<SqlValue> wheres)
		{
			database.Query(creator.DeleteRow(table, wheres));
		}
	}
}
