# Eventually
An exercise in eventual consistency.

## Overview

The console app will setup both a data entry service and an annotation service. Inputs are set to define the number of data fields, and the rates of entry, retries, and failures.

In this case, we have a domain model of two distributed systems using a pub/sub model that ends with an ackowledgement.
1. A data entry system that produces instances of a "First Class Field" type that require annotation by an external service
   
  - This service will continually publish any "First Class Field" that does not yet have an annotation. We assume all fields require annotation.
2. An annotation service that will attempt to find an existing annotation or generate one for a given field upon notification
   
  - This service will publish annotations upon fetch/create events, as well as continually publish annotations that have not yet been acknowledged

The business goal is to **confidently** annotate all fields in the data entry service. Using a "fire-and-forget" delivery model, the program simulates failure at any of the communication points, and provides both an event-based integration as well as a state-based scheduled retry integration. We achieve the following goals:
1. Limited afferent and efferent coupling between the two services
2. No need to assume or even attempt to produce a *perfect* messaging technology
3. A business-state based reconciliation between two systems that adheres to principles limited coupling

The model produces an eventually consistent result with a mathematical reliability within a certain range of tolerance. I haven't yet bothered to define the formula for that, but I am absolutely confident that it exists. Some of the timing and rates of state change grow unpredictable due to the number of concurrent Tasks and the program's inability to run as an actual distributed system. But, this is good enough for a demonstration of the principles.

## Technical Flow

Starting from the Program.cs file in the Eventually.Console project, the code setups of the following in sequence:

* An instance of FieldService, not in a using block for automatic disposal, since it is injected into the DataEntryService. FieldService responsibilities are:
  * Generate new instances of FirstClassField type every specified TimeSpan and up to a specified maximum count
  * Expose non-annotated fields as a ReadOnlyCollection in its GetUnannotatedFields method
  * Save Annotations to specified field Ids, with a specified rate of failure using Interlock.Increment and a divisor
    * Interlock.Increment provides a thread-safe, atomic incrementer. Combined with the divisor/remainder pattern, should provide an accurate rate of failure every X iteration


* An instance of the DataEntryService, injecting the above FieldService. DataEntryService is put into a using block since it implements IDisposable (due to the use of the Reactive Observable Interval). PublishService is intended to expose a public API, whereas FieldService is intended to be internal and likened to a data access implementation.
  * Exposes a "public" API to add an Annotation to a FieldId. If this succeeds, the Acknowledge function will also be invoked if the save to FieldService succeeds
      * The flow of Program.cs is a little odd, since we're limited by being in a single process. Once StartPublishing is called, DataEntryService will perform:
        * Sets the Acknowledge function's reference to the specified Func
        * Continuously republish all non-annotated fields, as provided by the FieldService, for annotation in a fire-and-forget manner


* An instance of the AnnotationService, which has the responsibility of attempting to generate, store, and publish annotations for field Ids.
  * The flow of Program.cs is a little odd, since we're limited by being in a single process. Once this instance is constructed, two of its async methods are passed into the DataEntryService's StartPublishing method:
    * Annotate, which will simulate a rate of failure similarly to the FieldService. If successful, this method will find all existing Annotations for the field ID, or generate a new one if none were found, and then publish the latest one to the Publish Annotation func as a fire-and-forget async call, which was injected to be the DataEntryService's Annotate method
    * Acknowledge, which also simulates a rate of failure. If successful, this method will update the specified Annotation ID to be "Acknowledged" and consider its workflow completed.
  * Upon construction, AnnotationService will continuously republish all unacknowledged Annotations to the same Publish Annotation function that is referenced in the Annotate method

The console app in Program.cs will also use Reactive Extension's Observable.Interval method, which is used widely in the app to create continuous, scheduled processes that run in the background. Every second, the console app will poll the state of both the Data Entry Service and the Annotation Service to output the counts. The rate of change will be determined by the specified field count and rates of failure. Eventually, consistency is reached no matter what (if one is patient enough) and the user is provided a friendly and exclamatory message stating as much.

## Business Flow

![Eventually Consistent Flow](./doc/Eventually.svg "Eventually Consistent Flow")
