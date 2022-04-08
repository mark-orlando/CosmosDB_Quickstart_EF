using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace todo {
    class Program {
        private const string EndpointUrl = "https://localhost:8081/";
        private const string AuthorizationKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private const string DatabaseId = "FamilyDatabase-EF6";

        private static readonly DbContextOptions<FamilyContext> Options = new DbContextOptionsBuilder<FamilyContext>()
            .UseCosmos(EndpointUrl, AuthorizationKey, DatabaseId)
            .Options;

        private static FamilyContext GetContext() => new FamilyContext(Options);

        static async Task Main(string[] args) {
            //await Program.CreateDatabaseAsync();
            //await Program.AddItemsToContainerAsync();
            //await Program.QueryItemsAsync();
            //await Program.ReplaceFamilyItemAsync();
            //await Program.DeleteFamilyItemAsync();
            await Program.DeleteDatabaseAndCleanupAsync();

            Console.WriteLine("Click any key to exit...");
            Console.ReadLine();
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private static async Task CreateDatabaseAsync() {
            using var context = GetContext();
            await context.Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// Add Family items to the container
        /// </summary>
        private static async Task AddItemsToContainerAsync() {

            // Create a family object for the Adamski family.
            Family adamskiFamily = new Family {
                Id = "Adamski.1",
                LastName = "Adamski",
                Parents = new Parent[] {
                    new Parent { FirstName = "Victor" },
                    new Parent { FirstName = "Cynthia" }
                },
                Children = new Child[] {
                    new Child {
                        FirstName = "James Thomas",
                        Gender = "male",
                        Grade = 5,
                        Pets = new Pet[] {
                            new Pet { GivenName = "Snickers" }
                        }
                    }
                },
                Address = new Address { State = "IL", County = "Kane", City = "Carpentersville" },
                IsRegistered = false
            };

            using var context = GetContext();

            try {
                var family = await context.Families.SingleAsync(f => f.Id == adamskiFamily.Id);
                Console.WriteLine("Item in database with id: {0} already exists\n", family.Id);
            } catch {
                context.Add(adamskiFamily);
                await context.SaveChangesAsync();
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse.
                Console.WriteLine("Created item in database with id: {0}\n", adamskiFamily.Id);
            }

            // Create a family object for the Orlando family.
            Family OrlandoFamily = new Family {
                Id = "Orlando.1983",
                LastName = "Orlando",
                Parents = new Parent[] {
                    new Parent { FamilyName = "Camel",   FirstName = "Nancy" },
                    new Parent { FamilyName = "Orlando", FirstName = "Mark" }
                },
                Children = new Child[] {
                    new Child {
                        FamilyName = "Orlando",
                        FirstName = "Megan",
                        Gender = "female",
                        Grade = 8,
                        Pets = new Pet[] {
                            new Pet { GivenName = "Blue" },
                            new Pet { GivenName = "Max" }
                        }
                    },
                    new Child {
                        FamilyName = "Orlando",
                        FirstName = "Nicholas",
                        Gender = "male",
                        Grade = 1
                    }
                },
                Address = new Address { State = "IL", County = "DuPage", City = "Villa Park" },
                IsRegistered = true
            };

            context.Add(OrlandoFamily);
            await context.SaveChangesAsync();

            Console.WriteLine("Created item in database with id: {0}\n", OrlandoFamily.Id);
        }

        /// <summary>
        /// Run a query (using Azure Cosmos DB SQL syntax) against the container
        /// </summary>
        private static async Task QueryItemsAsync() {
            using var context = GetContext();
            var familyQuery = context.Families.Where(f => f.LastName == "Adamski");

            Console.WriteLine("Running query: {0}\n", familyQuery.ToQueryString());

            var families = await familyQuery.ToListAsync();

            foreach (Family family in families) {
                Console.WriteLine("\tRead {0}\n", family);
            }
        }

        /// <summary>
        /// Replace an item in the container
        /// </summary>
        private static async Task ReplaceFamilyItemAsync() {
            using var context = GetContext();
            var itemBody = await context.Families.WithPartitionKey("Orlando").SingleAsync(f => f.Id == "Orlando.1983");

            // update registration status from false to true
            itemBody.IsRegistered = true;

            // update grade of child
            itemBody.Children[0].Grade = 6;

            await context.SaveChangesAsync();
            Console.WriteLine("Updated Family [{0},{1}].\n \tBody is now: {2}\n", itemBody.LastName, itemBody.Id, itemBody);
        }

        /// <summary>
        /// Delete an item in the container
        /// </summary>
        private static async Task DeleteFamilyItemAsync() {
            using var context = GetContext();

            var familyToDelete = new Family { Id = "Orlando.1983", LastName = "Orlando" };
            context.Entry(familyToDelete).State = EntityState.Deleted;
            await context.SaveChangesAsync();

            Console.WriteLine("Deleted Family [{0},{1}]\n", familyToDelete.LastName, familyToDelete.Id);
        }

        /// <summary>
        /// Delete the database and dispose of the Cosmos Client instance
        /// </summary>
        private static async Task DeleteDatabaseAndCleanupAsync() {
            using var context = GetContext();
            await context.Database.EnsureDeletedAsync();
            Console.WriteLine("Deleted Database: {0}\n", Program.DatabaseId);
        }
    }
}
