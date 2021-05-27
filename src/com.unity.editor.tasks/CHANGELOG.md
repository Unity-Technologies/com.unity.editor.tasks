# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [VERSION] - DATE

### Changed
- Bumped version to 2.1.x.

## [1.2.32] - 2020-02-12

### Changed
- Made ProcessManager methods virtual so functionality can be reused.
- Made tasks with UI affinity to be invoked faster.

## [1.2.18] - 2019-12-17

### Added
- Added helper for a completed ITask<T> instance.

### Changed
- Bumped .NET reference assembly version.

## [1.2.17] - 2019-12-17

### Fixed
- Fixed a bug in BaseOutputProcess default string processing.

## [1.2.16] - 2019-12-17

### Added
- Added process tasks that return lists of data.
- Added IsChainExclusive helper method.

### Changed
- Moved CancellationToken argument to the end of all constructors.
- Restored some functionality to the base output processor.

## [1.2.12] - 2019-12-16

### Changed
- Renamed NuGet packages to match the PackMan naming.

### Fixed
- Fixed Unity warning about namespace changing with defines.

## [1.2.10] - 2019-12-12

### Fixed
- Fixed URL in package.json.

## [1.2.9] - 2019-12-12

### Changed
- Modified code to throw real exception when rethrowing aggregate exceptions.
- Made TaskData public as it's a useful stub class for handling progress reporting.
- Made TaskManager.UIScheduler no longer settable. If you want to use a custom UI scheduler, use a custom synchronization context. If you need a custom single-threaded synchronization context, use the `ThreadSynchronizationContext` class.

## [1.2.8] - 2019-12-11

### Fixed
- Fixed cancelling and disposing schedulers.

## [1.2.7] - 2019-12-10

### Added
- Added a threadlocal static field with the current task in TaskBase.
- Added support for running tasks on custom schedulers.
- Added Native/.NET/Mono ProcessTask classes to simplify running processes.
- Added a test to show how to insert tasks in a chain.

### Fixed
- Fixed disposing of resources.
- Fixed potential type collisions. 

## [1.2.1] - 2019-12-05

### Added
- Added .NET/Mono process tasks.

### Changed
- Changed task extension methods to be easier to use.

`Then` and `ThenInUI` now take delegates that either have just the data, or success+exception+data, so it's easier to chain tasks without having to ignore arguments. Since `Then` methods by default only run the task if the previous one succeeded, there's no reason to make the `success` argument mandatory (it will mostly always be true).

Also add `ThenInExclusive` extension methods, because `Then` methods by default run in the Concurrent scheduler, so it makes sense to have a method for the exclusive one, given that there's already one for the UI scheduler.

This also adds `TaskManager.With` extension methods that return `ITask` instances in the same way as `Then` methods. This makes it easy to create tasks without invoking the constructors explicitely, with the syntax `TaskManager.With(DoSomething).Then(DoSomethingElse)`.

Extension methods that wrap async/await tasks (TPLTask objects) are now called `ThenAsync`, to make sure they don't get confused with with overloads that create `Func<T>` tasks.

## [1.2.0] - 2019-12-04

### This release has a number of interface changes and new types.

### Added
- Added more extension points into process manager.

### Fixed
- Fixed threading issues.

## [1.1.17] - 2019-12-03

### Fixed
- Fixed running processes, catching exceptions and producing output.

## [1.1.16] - 2019-12-01

### Fixed
- Fixed tests under Unity.

## [1.1.10] - 2019-11-30

### Added
- Added symbols for NuGet packages.
- Added Unity application contents path to the default Environment initialization.
- Added documentation.

### Fixed
- Fixed schedulers for running processes.
- Fixed task constructors.

## [1.1.4] - 2019-11-20

### Fixed
- Fixed native async/await support.

### This is the first release of *Unity Package Unity Editor Tasks*.

Unity Editor Tasks is a [TPL](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl) based threading package that simplifies running asynchronous code with explicit thread and scheduler settings.