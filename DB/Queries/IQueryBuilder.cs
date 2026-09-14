/*
TShock, a server mod for Terraria
Copyright (C) 2011-2025 Pryaxis & TShock Contributors

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

using System.Collections.Generic;
using MySqlConnector;

namespace TShockAPI.DB.Queries;

/// <summary>
/// Interface for various SQL related utilities.
/// </summary>
public interface IQueryBuilder
{
	/// <summary>
	/// Creates a table from a SqlTable object.
	/// </summary>
	/// <param name="table">The SqlTable to create the table from</param>
	/// <returns>The sql query for the table creation.</returns>
	string CreateTable(SqlTable table);

	/// <summary>
	/// Alter a table from source to destination
	/// </summary>
	/// <param name="from">Must have name and column names. Column types are not required</param>
	/// <param name="to">Must have column names and column types.</param>
	/// <returns>The SQL Query</returns>
	string AlterTable(SqlTable from, SqlTable to);

	/// <summary>
	/// Converts the MySqlDbType enum to it's string representation.
	/// </summary>
	/// <param name="type">The MySqlDbType type</param>
	/// <param name="length">The length of the datatype</param>
	/// <returns>The string representation</returns>
	string DbTypeToString(MySqlDbType type, int? length);

	/// <summary>
	/// A UPDATE Query
	/// </summary>
	/// <param name="table">The table to update</param>
	/// <param name="values">The values to change</param>
	/// <param name="wheres"></param>
	/// <returns>The SQL query</returns>
	string UpdateValue(string table, List<SqlValue> values, List<SqlValue> wheres);

	/// <summary>
	/// A INSERT query
	/// </summary>
	/// <param name="table">The table to insert to</param>
	/// <param name="values"></param>
	/// <returns>The SQL Query</returns>
	string InsertValues(string table, List<SqlValue> values);

	/// <summary>
	/// A SELECT query to get all columns
	/// </summary>
	/// <param name="table">The table to select from</param>
	/// <param name="wheres"></param>
	/// <returns>The SQL query</returns>
	string ReadColumn(string table, List<SqlValue> wheres);

	/// <summary>
	/// Deletes row(s).
	/// </summary>
	/// <param name="table">The table to delete the row from</param>
	/// <param name="wheres"></param>
	/// <returns>The SQL query</returns>
	string DeleteRow(string table, List<SqlValue> wheres);

	/// <summary>
	/// Renames the given table.
	/// </summary>
	/// <param name="from">Old name of the table</param>
	/// <param name="to">New name of the table</param>
	/// <returns>The sql query for renaming the table.</returns>
	string RenameTable(string from, string to);
}
