# Incubation
## Realistic Synthetic Data for ML Testing

`"built to break ML systems before users do"`

Incubation is a high-throughput synthetic data generator designed to stress-test machine learning pipelines, recommenders, and clustering models across *any* ML framework.
It produces large volumes of randomized — but statistically *structured* — customer and order records that mimic real-world dataset behavior, including long-tail SKU popularity, user segments, seasonality, missing values, and outliers.

**What gets generated**
- Millions of **order events** (for matrix factorization, recommenders, behavioral analysis)
- Matching **customer records** (for churn prediction, segmentation, and clustering)
- Coherent telecom-style usage fields, basket variance, and probabilistic churn labels
- Non-uniform distributions so models learn *real signal*, not uniform noise

**What stays stable**
- No assumptions about ONNX, pickle, Torch, or other model formats
- Core generation engine emits **opaque artifacts + standardized metadata**

**Configurable randomness**
Customer IDs, SKU pools, basket size, SKU quantities, recency, support interactions, and churn likelihood are all generated using reproducible shuffles (Fisher-Yates), Poisson/lognormal sampling, weighted selection, and noise injection.

**Output formats**
- CSV or JSON written to local disk, blob storage, or any external sink
- P.I.I. columns (e.g.: emails, names) can be excluded at export time without altering internal state
- Built for easy dataset versioning, validation, and plugin-based model evaluation

Incubation helps you test ML implementations under realistic conditions *before deploying to real users*, making it ideal for portfolios, benchmarks, and system hardening.


## Updates

---
### Additions
	- Non-uniform sampling
	- Latent person segments
	- Long-tail SKU weighting
	- Noise + outliers + nulls
	- Feature correlations
	- Probabilistic churn
	- Simple to integrate with existing ML pipelines

---

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