# DotJEM Fluent Routing for AspNetCore 2.0+

WIP! - This is still in it's very early stages and far from done.

The main features are:
 - Define Routes in a Fluent Syntax
 - Define Routes that point to Functions rather than Controllers.

# Examples

Adding the services:
```CSharp
services.AddFluentRouting();
```
### Controller Routes

Simple Routing to Controllers:
```CSharp
app.UseFluentRouter(router => {
    router.Route("api/stuff/{parameter?}").To<MyController>();
});
```

### Functional Routes
Minimal Hello World Route:
```CSharp
app.UseFluentRouter(router => {
    router.Route("hello").To(() => "Hello World");
});
```
