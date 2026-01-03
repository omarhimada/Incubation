using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Incubation.Models;

namespace Incubation.Storage {

    public interface ITrainingDataStorage {
        /// <summary>
        /// Asynchronously writes a CSV file containing new random orders based on the specified transactions.
        /// </summary>
        /// <param name="transactions">The collection of random transactions to use as the source for generating new orders. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteNewRandomOrdersCsvAsync(ManyRandomTransactions transactions);

        /// <summary>
        /// Asynchronously writes a new CSV file containing the specified customers, generating random data for each
        /// entry.
        /// </summary>
        /// <remarks>If includePiiColumns is set to true, the generated CSV will contain additional
        /// columns with sensitive customer information. Ensure appropriate handling of the output file to comply with
        /// privacy and data protection requirements.</remarks>
        /// <param name="customers">The list of customers to include in the CSV file. Cannot be null or empty.</param>
        /// <param name="includePiiColumns">true to include personally identifiable information (PII) columns in the CSV output; otherwise, false.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteNewRandomCustomersCsvAsync(List<Customer> customers, bool includePiiColumns);
    }
}
