/*******************************************************************************
* Copyright 2009-2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache2.0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Threading;

namespace AwsDynamoDBDataModelSample1
{
    public static class TableOperations
    {
        static readonly string[] SAMPLE_TABLE_NAMES = { "Actors", "Movies" };
        /// <summary>
        /// Creates all samples defined in SampleTables map
        /// </summary>
        /// <param name="client"></param>
        public static void CreateSampleTables(AmazonDynamoDBClient client)
        {
            Console.WriteLine("Getting list of tables");
            List<string> currentTables = client.ListTables().TableNames;
            Console.WriteLine("Number of tables: " + currentTables.Count);

            bool tablesAdded = false;
            if (!currentTables.Contains("Actors"))
            {
                Console.WriteLine("Table Actors does not exist, creating");
                client.CreateTable(new CreateTableRequest
                {
                    TableName = "Actors",
                    ProvisionedThroughput = new ProvisionedThroughput { ReadCapacityUnits = 3, WriteCapacityUnits = 1 },
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "Name",
                            KeyType = KeyType.HASH
                        }
                    },
                    AttributeDefinitions = new List<AttributeDefinition>
                    {
                        new AttributeDefinition { AttributeName = "Name", AttributeType = ScalarAttributeType.S }
                    }
                });
                tablesAdded = true;
            }

            if (!currentTables.Contains("Movies"))
            {
                Console.WriteLine("Table Movies does not exist, creating");
                client.CreateTable(new CreateTableRequest
                {
                    TableName = "Movies",
                    ProvisionedThroughput = new ProvisionedThroughput { ReadCapacityUnits = 3, WriteCapacityUnits = 1 },
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "Title",
                            KeyType = KeyType.HASH
                        },
                        new KeySchemaElement
                        {
                            AttributeName = "Released",
                            KeyType = KeyType.RANGE
                        }
                    },
                    AttributeDefinitions = new List<AttributeDefinition>
                    {
                        new AttributeDefinition { AttributeName = "Title", AttributeType = ScalarAttributeType.S },
                        new AttributeDefinition { AttributeName = "Released", AttributeType = ScalarAttributeType.S }
                    }
                });
                tablesAdded = true;
            }

            if (tablesAdded)
            {
                bool allActive;
                do
                {
                    allActive = true;
                    Console.WriteLine("While tables are still being created, sleeping for 5 seconds...");
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    foreach (var tableName in SAMPLE_TABLE_NAMES)
                    {
                        TableStatus tableStatus = GetTableStatus(client, tableName);
                        if (!object.Equals(tableStatus, TableStatus.ACTIVE))
                            allActive = false;
                    }
                } while (!allActive);
            }

            Console.WriteLine("All sample tables created");
        }

        /// <summary>
        /// Retrieves a table status. Returns empty string if table does not exist.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private static TableStatus GetTableStatus(AmazonDynamoDBClient client, string tableName)
        {
            try
            {
                var table = client.DescribeTable(new DescribeTableRequest { TableName = tableName }).Table;
                return (table == null) ? null : table.TableStatus;
            }
            catch (AmazonDynamoDBException db)
            {
                if (db.ErrorCode == "ResourceNotFoundException")
                    return string.Empty;
                throw;
            }
        }

        /// <summary>
        /// Deletes all sample tables
        /// </summary>
        /// <param name="client"></param>
        public static void DeleteSampleTables(AmazonDynamoDBClient client)
        {
            foreach (var table in SAMPLE_TABLE_NAMES)
            {
                Console.WriteLine("Deleting table " + table);
                client.DeleteTable(new DeleteTableRequest { TableName = table });
            }

            int remainingTables;
            do
            {
                Console.WriteLine("While sample tables still exist, sleeping for 5 seconds...");
                Thread.Sleep(TimeSpan.FromSeconds(5));

                Console.WriteLine("Getting list of tables");
                var currentTables = client.ListTables().TableNames;
                remainingTables = currentTables.Intersect(SAMPLE_TABLE_NAMES).Count();
            } while (remainingTables > 0);

            Console.WriteLine("Sample tables deleted");
        }
    }
}
