# Incubation
## Realistic Synthetic Data for ML Testing

`"built to break ML systems before users do"`

Incubation is a high-throughput synthetic data generator designed to stress-test machine learning pipelines, recommenders, regressions like Fast Tree or LightBGM, and clustering across *any* ML framework.
It produces large (configurable) volumes of randomized — yet statistically *structured* — customer and order records that mimic real-world dataset behavior, including long-tail SKU popularity, user segments, seasonality, missing values, and outliers.

**What gets generated**
- Millions of **orders** (transactions) including `CustomerId,Sku,Quantity,RecencyDays` and also expression lambdas for calculating `QuantitySignal` and `RecencyWeightFromDays`
	-  (Those two additions have proven excepptionally useful when transformed using a custom mapping and appended to the transformer).
- Matching **customer records** with about ~22 features. I currently use L-BGFS Optimization and KMeans++ with this output.
- Coherent telecom-style usage fields, basket variance, and probabilistic churn labels
- Non-uniform distributions so models learn *real signal*, not uniform noise
- Always random:
	- One execution of the program returns two .csv documents; one for orders and the other for transactions.

**Configurable randomness**
- Customer IDs, SKU pools, basket size, SKU quantities, recency, the `TauDays` used for calculating `RecencyWeightFromDays`, support interactions, and churn likelihood are all generated using reproducible shuffles (Fisher-Yates), Poisson/lognormal sampling, weighted selection, and noise injection.

**Output formats**
- CSV or JSON written to local disk, blob storage, or any external sink. If you're using this for ML you're probably want to configure it to output CSV.
- P.I.I. columns (e.g.: emails, names) can be excluded at export time without altering internal state. Simply set it to true or false.
- Built for easy dataset versioning, validation, and plugin-based model evaluation.

Incubation helps you test ML implementations under realistic conditions *before deploying to real users*, making it ideal for portfolios, benchmarks, and system hardening.

## Recent Updates
- Changed SKU from string to integer. 
- Removed cartesian product 
```
// Replacement is much more efficient
int total = (int)Math.Pow(10, Configuration.MaximumLengthOfSku) - 1;
List<int> allSku = Enumerable.Range(1, total).ToList();
```
- Added `RecencyDays` field to orders for temporal analysis.
- Added optional `QuantitySignal` and `RecencyWeightFromDays` methods if you're using the `random-orders` output. You can combine them in an estimator chain.
- Replaced SKU clustering with a surprising LLM-assisted optimization:
```
// 32-bit MurmurHash3 finalizer
x ^= x >> 16;
x *= 0x85ebca6bu;
x ^= x >> 13;
x *= 0xc2b2ae35u;
x ^= x >> 16;
```

### Additions
	- Non-uniform sampling
	- Latent person segments
	- Long-tail SKU weighting
	- Noise + outliers + nulls
	- Feature correlations
	- Probabilistic churn
	- Simple to integrate with existing ML pipelines with `ITrainingDataStorage`
	  - Implement your own `ITrainingDataStorage`. The included `WriteToLocal` simply uses a `StreamWriter` writes the data to your local device. 
	  - You could upload to a selected cloud provider or internal network instead.


### Replacements
	- Fisher-Yates replaced with RandEx
	- (Poisson & Gaussian distributing) 
	- Better performance on large datasets
---

### Examples

Default Configuration (`Debug`)
```
/out/customers_20260103_012620.csv		0.5 MB
/out/orders_20260103_012620.csv			0.5 MB
```
Generated 10,000 transactions and 5,809 customers in **0.80s**

---

Compiled Modified Configuration (`Release`)
```
/out/customers_20260103_014157.csv		9.2 MB	
/out/orders_20260103_014157.csv"		16  MB
```
Generated 300,000 transactions and 87,799 customers in **19.78s**
