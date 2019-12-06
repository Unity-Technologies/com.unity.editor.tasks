# Changelog

<!-- Do not change the line immediately below this comment, the build system will replace it with the actual version and date. -->

## [VERSION] - DATE

## [1.2.1] - 2019-12-05

- Refactor task extension methods to be easier to use. Add .net/mono process tasks

`Then` and `ThenInUI` now take delegates that either have just the data, or success+exception+data, so
it's easier to chain tasks without having to ignore arguments. Since `Then` methods by default only run the
task if the previous one succeeded, there's no reason to make the `success` argument mandatory (it will mostly
always be true).

Also add `ThenInExclusive` extension methods, because `Then` methods by default run in the Concurrent scheduler,
so it makes sense to have a method for the exclusive one, given that there's already one for the UI scheduler.

This also adds `TaskManager.With` extension methods that return `ITask` instances in the same way as `Then` methods.
This makes it easy to create tasks without invoking the constructors explicitely, with the syntax
`TaskManager.With(DoSomething).Then(DoSomethingElse)`.

Extension methods that wrap async/await tasks (TPLTask objects) are now called `ThenAsync`, to make sure
they don't get confused with with overloads that create `Func<T>` tasks.

## [1.2.0] - 2019-12-04

This release has a number of interface changes and new types.

- Add more extension points into process manager.
- Fix threading issues.

## [1.1.17] - 2019-12-03

- Fixes for running processes, catching exceptions and producing output

## [1.1.16] - 2019-12-01

- Fix tests under Unity

## [1.1.10] - 2019-11-30

- Add symbols for nuget packages
- #6 - Fix schedulers for running processes, clean up task constructors, add documentation.
- Add Unity application contents path to the default Environment initialization

## [1.1.4] - 2019-11-20

- Fix native async/await support