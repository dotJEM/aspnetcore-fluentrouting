# DotJEM Fluent Routing for AspNetCore 2.0+

WIP! - This is still in it's very early stages.

The main features are:
 - Define Routes in a Fluent Syntax
 - Define Routes that point to Functions rather than Controllers.


# Examples

Adding the services:
```C#
services.AddFluentRouting();
```

Simple Hello World
```C#
app.UseFluentRouter(router => {
    router.Route("hello").To(() => "Hello World");
});
```
