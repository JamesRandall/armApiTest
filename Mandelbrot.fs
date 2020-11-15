module Mandelbrot

open System.IO
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats

let render (maxIterations:int) (xMin:float) (xMax:float) (yMin:float) (yMax:float) (width:int) (height:int) =
  let getColor iteration =
    let ratio = (iteration |> float) / (maxIterations |> float)
    if ratio < 0.125 then
      (((ratio/0.125)*512.)+0.5) |> byte, 0uy, 0uy
    elif ratio < 0.250 then
      255uy, (((ratio - 0.125) / 0.125) * (512.) + 0.5) |> byte, 0uy
    elif ratio < 0.375 then
      ((1.0 - ((ratio - 0.250) / 0.125)) * (512.) + 0.5) |> byte, 255uy, 0uy
    elif ratio < 0.500 then
      0uy, 255uy, (((ratio - 0.375) / 0.125) * (512.) + 0.5) |> byte
    elif ratio < 0.625 then
      0uy, ((1.0 - ((ratio - 0.500) / 0.125)) * (512.) + 0.5) |> byte, 255uy
    elif ratio < 0.750 then
      (((ratio - 0.625) / 0.125) * (512.) + 0.5) |> byte, 0uy, 255uy
    elif ratio < 0.875 then
      255uy, (((ratio - 0.750) / 0.125) * (512.) + 0.5) |> byte, 255uy
    else
      ((1.0 - ((ratio - 0.875) / 0.125)) * (512.) + 0.5) |> byte,
      ((1.0 - ((ratio - 0.875) / 0.125)) * (512.) + 0.5) |> byte,
      ((1.0 - ((ratio - 0.875) / 0.125)) * (512.) + 0.5) |> byte      
  
  let xScale = (xMax - xMin) / (width |> float)
  let yScale = (yMax - yMin) / (height |> float)
  use image = new Image<Rgba32>(width, height)
  
  seq { for y=0 to image.Height-1 do for x=0 to image.Width-1 do (x,y) }
  |> Seq.iter(fun (sx,sy) ->
    let scaledX = (sx |> float) * xScale + xMin
    let scaledY = (sy |> float) * yScale + yMin
    
    let rec calculate iteration x y =
      let newX = x*x - y*y + scaledX
      let newY = 2.*x*y + scaledY
      if ((newX*newX) + (newY*newY)) <= (2.*2.) && iteration < maxIterations then
        calculate (iteration+1) newX newY
      else
        (iteration+1)
    
    let iteration = calculate 0 0. 0.
    if iteration = maxIterations then
      image.[sx,sy] <- Rgba32 (0uy,0uy,0uy)
    else
      let (r,g,b) = getColor iteration
      image.[sx,sy] <- Rgba32 (r,g,b)     
  )

  use memoryStream = new MemoryStream()
  image.SaveAsPng memoryStream
  memoryStream.Position <- 0L
  memoryStream.GetBuffer()