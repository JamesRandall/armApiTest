module simpleapitest.App

open System
open System.IO
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe

// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text : string
    }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open GiraffeViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "simpleapitest" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial () =
        h1 [] [ encodedText "simpleapitest" ]

    let index (model : Message) =
        [
            partial()
            p [] [ encodedText model.Text ]
            img [ _src "/mandelbrot" ; _width "1024" ; _height "768" ]
        ] |> layout

// ---------------------------------
// Web app
// ---------------------------------

let mandelbrotHandler : HttpHandler =
    fun (next : HttpFunc) (ctx:HttpContext) ->
        task {
            let mandelbrotBytes = Mandelbrot.render 64 -2.1 0.9 -1. 1. 1024 768
            return! ctx.WriteBytesAsync mandelbrotBytes
        }
        
let random = System.Random()

let asyncWorkload : HttpHandler =
    fun (next : HttpFunc) (ctx:HttpContext) ->
        task {
            let firstBytes = Mandelbrot.render 64 -2.1 0.9 -1. 1. 64 64
            do! Task.Delay (50 + random.Next(0,50))
            let secondBytes = Mandelbrot.render 64 -2.1 0.9 -1. 1. 128 128
            do! Task.Delay (100 + random.Next(0,25))
            
            let string = (sprintf "%d %d" firstBytes.Length secondBytes.Length)
            return! ctx.WriteTextAsync string
        }

let indexHandler (name : string) =
    let greetings = sprintf "Hello %s, from Giraffe!" name
    let model     = { Text = greetings }
    let view      = Views.index model
    htmlView view

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler "world"
                routef "/hello/%s" indexHandler
                route "/mandelbrot" >=> mandelbrotHandler
                route "/async" >=> asyncWorkload
                // shouldn't go in public source but will tear this down
                route "/loaderio-bb581c809a74c2b0bdc2598bbc967401.txt" >=> text "loaderio-bb581c809a74c2b0bdc2598bbc967401"
                route "/loaderio-7e66e1f1715d58e30e096ff1b04eae39.txt" >=> text "loaderio-7e66e1f1715d58e30e096ff1b04eae39"
                route "/loaderio-acb38b8646c69611e30fe2387d135ec8.txt" >=> text "loaderio-acb38b8646c69611e30fe2387d135ec8"
                route "/loaderio-db50d6564f32f12ed971711f34271f05.txt" >=> text "loaderio-db50d6564f32f12ed971711f34271f05"
            ]
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.EnvironmentName with
    | "Development" -> app.UseDeveloperExceptionPage()
    | _ -> app.UseGiraffeErrorHandler(errorHandler))
        //.UseHttpsRedirection()
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0