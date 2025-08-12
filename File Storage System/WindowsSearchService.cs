// WindowsSearchService.cs
// This class handles communication with the Windows Search index using OLE DB.

using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace FileFlow
{
    public class WindowsSearchService
    {
        /// <summary>
        /// Queries the Windows Search index for files and folders matching a search term within a specific path.
        /// </summary>
        /// <param name="searchPath">The root folder to search within (e.g., "\\LPMSRV\Group Files").</param>
        /// <param name="searchTerm">The text to search for in file or folder names.</param>
        /// <returns>A list of full paths to the matching items.</returns>
        public List<string> Search(string searchPath, string searchTerm)
        {
            var results = new List<string>();

            // The connection string for the Windows Search OLE DB provider.
            var connectionString = "Provider=Search.CollatorDSO;Extended Properties='Application=Windows';";

            // Escape single quotes in the search term to prevent SQL injection issues.
            var escapedSearchTerm = searchTerm.Replace("'", "''");

            // Build the SQL query.
            var sqlQuery = $@"
                SELECT System.ItemPathDisplay 
                FROM SYSTEMINDEX 
                WHERE SCOPE = 'file:{searchPath}' 
                AND (CONTAINS(System.FileName, '""*{escapedSearchTerm}*""') OR CONTAINS(System.ItemName, '""*{escapedSearchTerm}*""'))";

            try
            {
                using (var connection = new OleDbConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new OleDbCommand(sqlQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If Windows Search is not available or fails, this will prevent a crash.
                // The method will return an empty list.
            }

            return results;
        }
    }
}

