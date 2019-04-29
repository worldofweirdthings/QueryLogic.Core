# Data Access with QueryLogic

When accessing a database to retrieve data, there are two general ways to process the results of queries and SQL statements: connected and disconnected. Connected methods involve a reader which retrieves data when a connection to a database is opened and makes the stream available to you in real-time. In the .NET framework, this involves using classes implementing the IDataReader interface with code which iterates through the stream and allows the programmer to allocate data to an object. This works well in high performance applications where shaving off 30 to 100 milliseconds per database call would greatly speed up the application or solve a problem in a complex, intricately timed workflow. However, when it comes to testing, a connected data reader is difficult to mock and when mocked, the test harness would take a fair bit of effort to set up since every possible call needs to be properly set up for mocking, which make the unit tests impractically large in a large scale enterprise application.

Disconnected data sets execute a query or a statement, then allocate the results into an object in the .NET framework. Due to the need to allocate the data to an object in memory, there’s a notable overhead cost in .NET and the general layout of the rows and columns adds computational complexity to retrieving the mapping the data. But when it comes to testing and mocking, a disconnected data set is easy to set up and makes testing repositories practical for enterprise projects. Think of the differences between the two methods as building an Ikea table as someone hands you the pieces and read off the instructions vs. building the same table after someone already unpacked all the parts and laid out the instructions at your leisure. You can only test the build-as-you-go method if you have someone giving you the pieces and instructions, while the get-then-build method can be set up for a dry run.

The solution is to fill a data set using a reader and present it to the programmer as a lightweight data structure with constant access time and retrieve data from it without boxing and unboxing data types, which also adds overhead. Instead, data should be retrieved, parsed into the appropriate data types, housed in the lightweight structure as a generic object, and simply cast since we know it was read properly. It can then be retrieved in constant time, cast into its own type, then assigned to a variable or an object’s property. The structure housing the data can be mocked in a unit test with no external dependencies.

This is the idea behind QueryLogic. Essentially, it’s a fast, lightweight data abstractor which is meant to efficiently provide disconnected data sets without the programmer necessarily caring about whether the database being accessed is SQL Server, MySQL, Oracle, or any other RDBMS which a .NET library. After specifying the provide in a connection string, the programmer can dial in a procedure or a SQL or PL/SQL statement and run it almost as quickly as by using a data reader. It also comes with a set of utility methods for reading data and creating components for dynamic search queries, introducing a framework and pattern for complex entity searches.

# Basic Usage

To create a call to an existing stored procedure, use the QueryUtilityclass which contains various methods for working with the abstracted data handled by QueryLogic. You need to know the name of the stored procedure being accessed, and a method to get the connection string in your configuration file for the database where this procedure is compiled. If you don’t know this name and don’t provide it, the first available database connection will be used. The code will look as follows...

```
var command = QueryUtility.NewCommand("[command name]", "[database connection]");
```

To add parameters to the procedure call, you will invoke extension methods on the command object created earlier, one for a default, in parameter, and one for a specified out parameter without an "@" which will be created internally when QueryLogic resolves the correct database object...

```
command.AddParameter("[parameter name]", [object]);
command.OutParameter("[out parameter name]", [default object]);
```

The default object on the out parameter declaration is important because QueryLogic uses its type to determine and read the type of the expected return value. These parameters are not valid on either the QueryDataor QueryDataSet call since these calls presume a proper SQL select in the stored procedure and the abstractor discourages using them as anything but that. But then ModifyData is called on the command, the specified values are returned alongside an affected row count. Query commands return a Rows object, which as the name implies, is a collection of database rows retrieved by the stored procedure. 

Inside of each Row object is an internal map which can be read and assigned using QueryUtility methods. When each cell is added to the internal map, it carries the name of the relevant database column, and the raw object retrieved from it, read in as its proper type to then be correctly cast by the utility rather boxed and unboxed, which would severely affect performance on a large data set. Nullible objects are read in as nulls and are handled with generics to return the proper nullible type in .NET when requested. Again, the goal is to prevent expensive type handling post-read. The full list of QueryUtilityread methods is as follows...

|method                                                   | return type   | default value if null|
----------------------------------------------------------|:--------------|:---------------------|
`QueryUtility.GetNullableFromRow<[type]?>(row, "column");`| `Nullible<T>` | `null`
`QueryUtility.GetByteFromRow(row, "column");`             | `byte`        | `0x00`
`QueryUtility.GetInt16FromRow(row, "column");`            | `short`       | `0`
`QueryUtility.GetInt32FromRow(row, "column");`            | `int`         | `0`
`QueryUtility.GetInt64FromRow(row, "column");`            | `long`        | `0`
`QueryUtility.GetStringFromRow(row, "column");`           | `string`      | `""`
`QueryUtility.GetDateTimeFromRow(row, "column");`         | `DateTime`    | `01/01/0001 00:00:00`
`QueryUtility.GetBytecodeFromRow(row, "column");`         | `byte[]`      | `[0x00]`
`QueryUtility.GetGuidFromRow(row, "column");`             | `Guid`        | `00000000-0000-...`
`QueryUtility.GetBooleanFromRow(row, "column");`          | `bool`        | `false`
`QueryUtility.GetDecimalFromRow(row, "column");`          | `decimal`     | `0`

Given these prerequisites, a full stored procedure call to retrieve an object and return it for further use
will involve the following code...

```
public IEnumerable<BasicFoo> GetFoo(Guid basicFooID)
{
    var queryBuilder = new QueryBuilder();
    var command = QueryUtility.NewCommand("dbGetBasicFoo", "FooDB");

    command.AddParameter("basicFooID", basicFooID);

    var rows = queryBuilder.QueryData(command);

    return rows.Select(r => new BasicFoo
    {
        ID = QueryUtility.GetGuidFromRow(r, "basicFooID"),
        Name = QueryUtility.GetStringFromRow(r, "basicFooName"),
        Created = QueryUtility.GetDateTimeFromRow(r, "createdDate")
    });
}
```

Notice that the LINQ extension method was used to create the collection of objects instead of a loop where each object is explicitly declared, populated, and added to a previously created collection. There are two reasons for encouraging using LINQ and lambdas. The first is that it makes the code terser and avoids unnecessary objects in memory even before they are Garbage Collected. While managed code frameworks like .NET and the JVM handle garbage collection and optimization automatically, which is a big part of the value proposition to programmers, this doesn’t mean it’s now a good idea to simply have extra objects or pointers lurking in memory. These objects still take up space and add overhead.

The second reason is for this approach is the encouragement to abstract finite bits of logic and call them as necessary, as is done in functional languages. With C# 6.0 and several previous versions, the .NET framework is clearly aiming for a hybrid OO/functional programming model which is meant to build leaner, more reusable code with fewer methods, as logic that may have been spun into a method of its own is now neatly contained in a lambda expression. Ultimately, this helps the Just-In-Time Optimizer, or JIT, to more efficiently inline method declaration and help the code be more performant on virtually any server or machine out of the box. Note that lambdas are not inherently faster than methods or the delegate implementation they neatly abstract and it’s entirely possible to write very poorly performing lambdas, especially when introducing large, complex objects into them from code outside of their scope, which breaks .NET’s ability to cache the expression for the number of times it’s being called in its parent object’s lifecycle. 

However, they do encourage programmers to write more efficient code which is easier and faster to execute if they are mindful of the tasks they’re putting into the expressions. So while it is possible to create a normal foreach loop with a Rows object and scan each Row to retrieve primitive values for entities, it should be avoided if possible unless the results and assignment logic need to be debugged before being wrapped back into a lambda expression. This is why the code recommended for handling multi-select stored procedures looks as follows...

```
public IEnumerable<ComplexFoo> GetFoo(Guid complexFooID)
{
    var queryBuilder = new QueryBuilder();
    var command = QueryUtility.NewCommand("dbGetComplexFoo", "FooDB");
    
    command.AddParameter("complexFooID", complexFooID);
    
    var data = queryBuilder.QueryDataSet(command);
    var complexFooBars = new ComplexM2M<Bar>();
    var complexFoos = data.First().Select(r => new BasicFoo
    {
        ID = QueryUtility.GetGuidFromRow(r, "complexFooID"),
        Name = QueryUtility.GetStringFromRow(r, "complexFooName"),
        Created = QueryUtility.GetDateTimeFromRow(r, "createdDate"),
        Bar = new List<Bar>();
    });
    
    data.Next().ForEach(r =>
    {
        var complexFooID = QueryUtility.GetGuidFromRow(r, "complexFooID");
        var bar = new Bar
        {
            ID = QueryUtility.GetGuidFromRow(r, "barID"),
            Name = QueryUtility.GetStringFromRow(r, "barName")
        }
    
        complexFooBars.AddM2M(complexFooID, bar);
    });

    foreach (var complexFoo in complexFoos.Where(f => complexFooBars.Contains(f.ID)))
    {
        complexFoo.Bars = complexFooBars[complexFoo.ID];
    }
}
```

You may notice that just after mentioning that lambda performance is impacted by passing in an outside scope object, the provided example references such an object. There are two reasons for this. The first is that the object is necessary for mapping the child and parent objects. How it does so will be explained shortly. The second is that the object being passed in is lean and uses a Dictionary and generics for its internal mapping between objects. The leaner the object and the better its performance, the less of a hit is being taken by the lambda, and while the performance would have been similar to a foreach loop, the lambda style keeps it similarly terse to the initial retrieval of the parent object. Here, implementation is being modified to better fit convention so ultimately, it’s easier to find one’s way around the code and make necessary changes over time.

Unlike the previous single selectstored procedure which returns a Rows object, a QueryDataSet call returns a DataMap object, which is basically a carefully managed collection of Row objects. Each call returns a Rows object which symbolizes the results of a select statement. The DataMap methods and their functionality are as follows...

|method       | description                                                                                                                                              |
|-------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------|
|`First();`   | Returns the first result set in the data map and does not advance the tracking of which result set will be returned in the next call                     |
|`Next();`    | Returns the next result set in the data map and advances the current collection of selected data forward so the next calls returns the next result set   |
|`Previous();`| Returns the last result set in the data map and rolls back the current collection of selected data backward so the next calls returns the next result set|
|`Last();`    | Returns the first result set in the data map and does not change the tracking of which result set will be returned in the next call                      |

Keep in mind that the order of the Rows in the DataMap object matches the order of the select statements in the stored procedure so the code should be planned accordingly. This leads us to the `ComplexM2M<T>` object used in the sample code earlier to handle mapping of objects created from subsequent queries, which is a utility class that handles many-to-many mapping (hence the name) when the objects are being assembled. Internally, it holds a hash map that uses the ID of a parent as a key and a generic list of child objects as each value. As child objects are added, it determines into which value slot it belongs to abstract away the logic to avoid key collisions, and provides several helpers for more complex mapping that involves further trips to the database or trivial parallelization (which will be covered in the next section on advanced usage of QueryLogic utilities). These helpers are ...

method                          | description                                                                                            | return value|
|-------------------------------|:-------------------------------------------------------------------------------------------------------|:------------|
`AddM2M(Guid/int key, T value);`| Add an instance of a many-to-many relationship using the parent’s ID as a key for the child objects    | `void`      |
`Contains(Guid?/int? key);`     | Checks if a parent element’sIDhas been added to the internal relationship map                          | `bool`      |
`Any();`                        | Checks if any meant to many relationships have been mapped by the object at call time                  | `bool`      |
`Flatten();`                    | Flattens all child elements into a single collection, similar to LINQ’s `SelectMany();` method         | `List<T>`   |
`[Guid?/int? key]`              | Returns all children of the specified parent element as a collection which implements `IEnumerable<T>` | `List<T>`   |

We will deal with these methods in more detail when covering parallelization during queries. Most of the complex helpers only come into play when retrieving objects rather than performing non-queries. QueryLogic allows for very simple code when creating, updating, and deleting data. If we wanted to save one of our objects from an earlier example and retrieve the new ID we created in the stored procedure for use elsewhere in the application, the code would look as follows...

public int SaveFoo(Foo foo, Guid userID)
{
    var queryBuilder = new QueryBuilder();
    var command = QueryUtility.NewCommand("dbSaveFoo", "FooDB");
    
    command.AddParameter("fooName", foo.Name);
    command.AddParameter("userID", userID);
    command.OutParameter("newFooID", Guid.Empty);
    
    var output = queryBuilder.ModifyData(command);
    
    return QueryUtility.GetGuidFromRow(output, "newFooID");
}

Note that there’s no limitation to how many out parameters can be added to a QueryLogic command and all returned values are indexed with the parameters’ names. If your procedure does not return a parameterbut simply deletes an entity or a row, you can still check the results by calling the constant output parameter "rows_affected" to make sure the command was executed as expected. The code would look similarto the previous example with the exception of the string constant being read...

```
public int DeleteFoo(Guid fooID)
{
    var queryBuilder = new QueryBuilder();
    var command = QueryUtility.NewCommand("dbDeleteFoo", "FooDB");
    
    command.AddParameter("fooName", fooID);
    
    var output = queryBuilder.ModifyData(command);
    
    return QueryUtility.GetGuidFromRow(output, "rows_affected");
}
```

Again, if necessary, the "rows_affected" parameter is always present on a non-query command even if out parameters have been added. This is provided for additional validation purposes even though they should not be necessary and may actually cause false validation in a web environment where it’s very possible for an entity to be deleted milliseconds before your call to the procedure clears. The entity may be gone as is expected and desired, but it will appear as if it was never actually present. QueryLogic also supports inline SQL statements and the same methods as for stored procedures will be available. In fact, internally, the inline statements and stored procedures aren’t treated differently aside from a command object flag for the database to which it’s talking so the database engine knows how to properly execute the command it’s going to get. This is because then the engine makes the call, it has to compile it with different syntax when executing the code. To use inline SQL instead of a stored procedure, simply call the NewInlineSql method and use the appropriate QueryBuilder call like so...

```
var queryBuilder = new QueryBuilder();
var sql = QueryUtility.NewInlineSql("select f.fooID, f.fooName from dbFoo f", "FooDB");
var rows = queryBuilder.QueryData(sql);
```

You can then proceed to map the retrieved rows to individual objects using the patterns shown in the examples above. For complex objects with multiple many to many relationships or for which special mapping logic is required, QueryLogic provides several additional utilities.

# Advanced Usage

Enterprise applications are well known for large, complex objects which can be very computationally expensive to retrieve. This is why QueryLogic has a utility method for smart parallelization and can be leveraged with the proper setup. Please note that this applies only to trivial parallelization as several other .NET libraries would have to be used to maintain concurrency and avoid race conditions, i.e. the built in ConcurrentBag<T> class under the System.Collections.Concurrent namespace. Since these are intended for fine-tuned, high performance, high-volume algorithms, and will work best when retrieving over 1,000 entities, they should generally be discouraged from use in basic data retrieval. Paging would turn their use into an unnecessary burden that will compromise performance. Instead, consider the code below which retrieves and maps data to a complex object...

```
public IEnumerable<Customer> GetCustomers(IEnumerable<Guid> customerIDs)
{
    var actions = new List<Action>();
    var customers = new List<Customer>();
    var addresses = new ComplexM2M<Address>();
    var phones = new ComplexM2M<PhoneNumber>();
    var emails = new ComplexM2M<Email>();
    var queryBuilder = new QueryBuilder();
    var command = QueryUtility.NewCommand("dbGetCustomers","DB");
    
    action.Add(() =>
    {
        foreach (var customer in customers.Where(c => addresses.Contains(c.ID))) customer.Addresses = addresses[customer.ID];
    });
    
    action.Add(() =>
    {
        foreach (var customer in customers.Where(c => phones.Contains(c.ID))) customer.Phones = phones[customer.ID];
    });
    
    action.Add(() =>
    {
        foreach (var customer in customers.Where(c => emails.Contains(c.ID))) customer.Addresses = emails[customer.ID];
    });
    
    command.AddParameter("customerIDs", SqlTypeUtility.Compose(customerIDs));
    
    var data = queryBuilder.QueryDataSet(command);
    
    customers.AddRange(data.First().Select(r => new Customer
    {
        ID = QueryUtility.GetGuidFromRow(r, "customerID"),
        Name = QueryUtility.GetStringFromRow(r, "customerName")
    });
    
    data.Next().ForEach(r =>
    {
        var customerID = QueryUtility.GetGuidFromRow(r, "customerID");
        var address = new Address
        {
            Line1 = QueryUtility.GetStringFromRow(r, "addressLine1"),
            Line2 = QueryUtility.GetStringFromRow(r, "addressLine2"),
            City = QueryUtility.GetStringFromRow(r, "addressCity"),
            State = QueryUtility.GetStringFromRow(r, "addressState"),
            ZipCode = QueryUtility.GetStringFromRow(r, "addressZipCode"),
        }
        addresses.AddM2M(customerID, address);
    });

    data.Next().ForEach(r =>
    {
        var customerID = QueryUtility.GetGuidFromRow(r, "customerID");
        var phone = new PhoneNumber
        {
            Number = QueryUtility.GetStringFromRow(r, "phoneNo"),
            Extension = QueryUtility.GetStringFromRow(r, "phoneExt")
        }
        
        phones.AddM2M(customerID, phone);
    });
        
    data.Next().ForEach(r =>
    {
        var customerID = QueryUtility.GetGuidFromRow(r, "customerID");
        var email = new Email
        {
            Address = QueryUtility.GetStringFromRow(r, "emailAddress"),
            IsActive = QueryUtility.GetBooleanFromRow(r, "isActive")
        }
        
        emails.AddM2M(customerID, email);
    });
    
    actions.TryParallelize();
    
    return customers;
}
```

In this example, a customer object had three many to many relationships which were independent form each other and could have been mapped simultaneously. To take advantage of that fact, we created the collection of Action instances to which we added our lambdas mapping the children to their parent, just as we did in an earlier example. However, because we have multiple relationships to map, we invoke the `TryParallelize();` extension method provided by QueryLogic which considers the number of lambdas in the collection and whether it makes sense to run them sequentially or open a separate thread to run each one. This avoids creating extra threads if the mapping is conditional, to be ran only when a specific flag requesting it is passed in, and just one lambda ends up being added to the collection. This allows for more complex parallelization when dealing with interop scenarios, such as requests from a third party using an API which requests a manifest of what parts of an object to return in the response such as...

```
public IEnumerable<Customer> GetCustomers(IEnumerable<Guid> customerIDs, string[] manifest)
{
    var actions = new Dictionary<string, Action>();
    var customers = new List<Customer>();
    var queryBuilder = new QueryBuilder();
    var command = QueryUtility.NewCommand("dbGetCustomers","DB");
    
    actions.Add(CustomerManifest.Addresses, () =>
    {
        var addresses = new ComplexM2M<Address>();
        data[1].ForEach(r =>
        {
            var customerID = QueryUtility.GetGuidFromRow(r, "customerID");
            var address = new Address
            {
                Line1 = QueryUtility.GetStringFromRow(r, "addressLine1"),
                Line2 = QueryUtility.GetStringFromRow(r, "addressLine2"),
                City = QueryUtility.GetStringFromRow(r, "addressCity"),
                State = QueryUtility.GetStringFromRow(r, "addressState"),
                ZipCode = QueryUtility.GetStringFromRow(r, "addressZipCode"),
            }
        
            addresses.AddM2M(customerID, address);
        });
    
        foreach (var customer in customers.Where(c => addresses.Contains(c.ID)))
        {
            customer.Addresses = addresses[customer.ID];
        }
    });
    
    actions.Add(CustomerManifest.PhoneNos, () =>
    {
        var phones = new ComplexM2M<PhoneNumber>();
        
        data[2].ForEach(r =>
        {
            var customerID = QueryUtility.GetGuidFromRow(r, "customerID");
            var phone = new PhoneNumber
            {
            Number = QueryUtility.GetStringFromRow(r, "phoneNo"),
            Extension = QueryUtility.GetStringFromRow(r, "phoneExt")
            }
            
            phones.AddM2M(customerID, phone);
        });
        
        foreach (var customer in customers.Where(c => phones.Contains(c.ID)))
        {
            customer.Phones = phones[customer.ID];
        }
    });
    
    actions.Add(CustomerManifest.Emails, () =>
    {
        var emails = new ComplexM2M<Email>();
        
        data[3].ForEach(r =>
        {
            var customerID = QueryUtility.GetGuidFromRow(r, "customerID");
            var email = new Email
            {
                Address = QueryUtility.GetStringFromRow(r, "emailAddress"),
                IsActive = QueryUtility.GetBooleanFromRow(r, "isActive")
            }
            
            emails.AddM2M(customerID, email);
        });
        
        foreach (var customer in customers.Where(c => emails.Contains(c.ID)))
        {
            customer.Addresses = emails[customer.ID];
        }
    });
    
    // ... retrieve and assign customer data as per the previous example ...
    actions.Where(a => manifest.Any(e => a.Key == e)).TryParallelize();
}
```

Code like this allows you to create and maintain a single instance of an entity’s child manifest and fails gracefully should the user requesting the data request the entity with a typo or outright invalid string. Instead of returning an error message or throwing an exception, the result simply omits the incorrect manifest member. Please note that the SqlTypeUtility class is that of the code using QueryLogic, not a tool provided by it. And while the benefit of QueryLogic seems restricted to its extension for smarter and more careful parallelization, there’s more to it than that. It provided a disconnected data set that can be used in its own thread in parallel with others to assemble complex entities faster, freeing the programmer from having to maintain an expensive open connection to the database and without the overhead imposed by the built-in .NET DataSet class and its many complex children.

In an alternative scenario, in which only the IDs of the child object were returned by the stored procedure and had to be retrieved in full from a repository (a class responsible for mapping database rows to application entities in a typical enterprise design pattern), we can leverage the `ComplexM2M<T>` class’ utility methods as follows...

```
actions.Add(CustomerManifest.PhoneNos, () =>
{
    var phones = new ComplexM2M<PhoneNumber>();
    
    data.[2].ForEach(r =>
    {
        var customerID = QueryUtility.GetGuidFromRow(r, "customerID");
        var phone = new PhoneNumber { ID = QueryUtility.GetGuidFromRow(r, "phoneID") };
        
        phones.AddM2M(customerID, phone);
    });
    
    if (!phones.Any()) return;
    
    var phoneIDs = phones.Flatten().Select(p => p.ID);
    var phoneNums = _phoneNumberRepository.GetPhoneNumbers(phoneIDs);
    
    foreach (var customer in customers.Where(c => phones.Contains(c.ID)))
    {
        var customerPhoneIDs = phones[customer.ID].Select(p => p.ID);
        var customerPhoneNums = phoneNums.Where(p => customerPhoneIDs.Any(i => p.ID == i));
        
        customer.Phones = customerPhoneNums;
    }
});
```

One major caveat to keep in mind when doing this is not to add the same repository to more than one lambda since which will interfere with the creation and maintenance of the threads by injecting pointers to a shared object on which they’ll now be dependent. Good design would mandate that repositories are domain and entity specific, and when injected are there to deal only with one type of entity. If the aforementioned design rules are not followed, the operations would complete, but instead of earning a performance boost, they would actually be slower than sequential execution due to the extra resources needed to create and maintain the intertwined threads. Proper parallelization is actually a complicated feat in .NET which requires a good deal of foresight. The built-in JIT optimizers and the operating system on which the application’s bytecode runs, already perform rudimentary, well understood parallelization implemented by experts in the approach. When trying to gain a performance boost by multi-threading, your goal is not to improve on the existing optimizations, but to stay out of their way.