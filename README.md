# Eventually
An exercise in eventual consistency.

The console app will setup both a data entry service and an annotation service.

Inputs are set to define the number of data fields, and the rates of entry, retries, and failures.

The business goal is to *confidently* annotate all data fields on the data entry service

The model produces an eventually consistent result with a mathematical reliability within a certain range of tolerance. I haven't yet bothered to define the formula for that.