# EasyRun


This extension makes it easy to run and debug microservices using Tye or
multiple startup projects in Visual Studio.

Download this extension from the VS Gallery:
- [Visual Studio 2019 version](https://marketplace.visualstudio.com/items?itemName=BrianVraamark.EasyRun)
- [Visual Studio 2022 version](https://marketplace.visualstudio.com/items?itemName=BrianVraamark.EasyRun64Bit)

# Prerequisites

If you want to use Tye to run and debug your application, you need a few things
in your development environment.

### Tye

Project Tye is the key component to get everything working. Install the latest
version by following the guide on <https://github.com/dotnet/tye/blob/main/docs/getting_started.md>

### .NET Core 3.1

Did you remember to install .NET core 3.1 when you followed the Tye installation guide?

### Docker

Tye requires Docker installed. You can download and install Docker from <https://docs.docker.com/get-docker>

# Features

-   Run and Debug projects in Visual Studio
-   Multiple profiles with different configurations.
-   Auto-generate temporary Tye YAML file outside your Git Repository.
-   Use encrypted Secret Environment Variables (e.g., ConnectionString and password) outside your Git Repository.
-   Auto attaches Visual Studio debugger when you run your projects.
-   RegEx filter to include/exclude projects.

# Profiles

There are two different profiles you can use. Tye and Visual Studio. You can
choose which one when you create or edit a profile.

![Add new profile](Doc/Add-new-profile.png)

## Profile settings file and secrets

All profiles are stored in a file named `<solution-name>.EasyRun.json` in the
same folder as your solution file.

Secret Environment Variables is not stored in that file. Instead, they live in
the `%AppData%/EasyRun` folder and are encrypted. Secrets are only exposed in the
`%Temp%/EasyRun` folder when Tye runs and gets deleted again when Tye stops.

## Visual Studio Profiles

The first section defines your profiles and the RegEx project filter. There is
no dedicated button for running your projects. Just do as you always do after
selecting one or multiple projects.

![Visual Studio Profiles](Doc/Profiles-VS.png)

## Tye Profiles

![Tye Profiles](Doc/Profiles-Tye.png)

For Tye, you have a few extra options.

For each Profile, you can:
-   Start and stop Tye.
-   Auto Attach the debugger.
-   Launch the Tye Dashboard.

### Save selections as defaults

To reduce Git changes (or what ever you use), there are two "save modes" in EasyRun. You can choose between:

A) Service selections will automatically be saved when profiles are saved. This is useful if you create many different profiles but rarely change the selections.

B) Selections are not saved when you save profiles. Instead, you have to click on the link "Save current selections as default" to save your selections. This is useful if you have few profiles and change the selections between running Tye.

You can change what works best for you in Visual Studio Options.

![Tye autosave selections](Doc/Tye-autosave-selections.png)

When the option is set to false, the manually save link is shown in the tool window.

![Tye save selections](Doc/Tye-save-selections.png)

### Tye profile configuration

Each Tye profile has a set of options.

![Tye profile configuration](Doc/Tye-configuration.png)

#### Wait on attach

If you check `Wait on attach` Tye will halt running the service until the
debugger is attached to the process.

#### Watch

If you check `Watch` Tye will restart your service if you make any code
changes.

#### Tye Dashboard port

You can set the Tye Dashboard port. Selecting a different port can be helpful if
you have Visual Studio opened more the once.

#### Logging

When selecting logging, you can choose between two types � with or without URL.
- When selecting a type **with URL**, you have to specify an URL to an existing
running logger (remote or local).

- If you select a type **without URL**, Tye will create or reuse a container in
Docker. What container depends on the logging type you choose.

## Tye service configuration

For each Tye service, the following options can be set.

![Tye service configuration](Doc/Tye-service-configuration.png)

#### Name

This is the name the service will have when running in Tye. The project name will
be used if the field is left blank.

#### Bindings

Each service can have one or more bindings. You define each binding on a
separate line. The binding format is `[name]:[protocol]:<port>`.

Name is optional if you only define one binding. You can then use the format
`::<port>`. Protocol is always optional.

#### Arguments

Here you can define command-line arguments to the service.

#### Env. Variables

Here you can define environment variables for the service. You define each
variable as a key/value pair on a separate line. The key and the value has to be
separated p� an equal sign.

#### Secret Variables

You define Secret Environment Variables like normal variables. The only
difference is that you have to click on the edit (pen) button to see and change
them.

#### Replicas

Here you can define how many replicas (instances) of the service you want to run
simultaneously.

### Tye service attach/detach debugger

When Tye is running it is possible to attach and detach the debugger to individual services
using the little bug icon.

![Tye service debugging](Doc/Tye-service-attach-debugger.png)