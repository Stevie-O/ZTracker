﻿module Graphics

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let [| boomerang_bmp; bow_bmp; magic_boomerang_bmp; raft_bmp; ladder_bmp; recorder_bmp; wand_bmp; red_candle_bmp; book_bmp; key_bmp; 
        silver_arrow_bmp; wood_arrow_bmp; red_ring_bmp; magic_shield_bmp; boom_book_bmp; 
        heart_container_bmp; power_bracelet_bmp; white_sword_bmp; ow_key_armos_bmp;
        brown_sword_bmp; magical_sword_bmp; blue_candle_bmp; blue_ring_bmp;
        ganon_bmp; zelda_bmp; bomb_bmp |] =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("icons7x7.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    [|  for i = 0 to bmp.Width/7 - 1 do
            let r = new System.Drawing.Bitmap(7*3,7*3)
            for px = 0 to 7*3-1 do
                for py = 0 to 7*3-1 do
                    r.SetPixel(px, py, bmp.GetPixel(px/3 + i*7, py/3))
            yield r
    |]

let emptyUnfoundTriforce_bmps, emptyFoundTriforce_bmps, fullTriforce_bmps, owHeartSkipped_bmp, owHeartEmpty_bmp, owHeartFull_bmp = 
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("icons10x10.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let all = [|
        for i = 0 to bmp.Width/10 - 1 do
            let r = new System.Drawing.Bitmap(10*3,10*3)
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    r.SetPixel(px, py, bmp.GetPixel(px/3 + i*10, py/3))
            yield r
        |]
    all.[0..7], all.[8..15], all.[16..23], all.[24], all.[25], all.[26]

let BMPtoImage(bmp:System.Drawing.Bitmap) =
    let ms = new System.IO.MemoryStream()
    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png)  // must be png (not bmp) to save transparency info
    let bmimage = new System.Windows.Media.Imaging.BitmapImage()
    bmimage.BeginInit()
    ms.Seek(0L, System.IO.SeekOrigin.Begin) |> ignore
    bmimage.StreamSource <- ms
    bmimage.EndInit()
    let i = new Image()
    i.Source <- bmimage
    i.Height <- float bmp.Height 
    i.Width <- float bmp.Width 
    i

let greyscale(bmp:System.Drawing.Bitmap) =
    let r = new System.Drawing.Bitmap(7*3,7*3)
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            let c = bmp.GetPixel(px,py)
            let avg = (int c.R + int c.G + int c.B) / 5  // not just average, but overall darker
            let avg = if avg = 0 then 0 else avg + 20    // never too dark
            let c = System.Drawing.Color.FromArgb(avg, avg, avg)
            r.SetPixel(px, py, c)
    r

let emptyZHelper =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ZHelperEmpty.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    for i = 0 to bmp.Width-1 do
        for j = 0 to bmp.Height-1 do
            let c = bmp.GetPixel(i,j)
            if false then //c.R = 53uy && c.G = 53uy && c.B = 53uy then
                bmp.SetPixel(i, j, System.Drawing.Color.Black)
    bmp
let fullZHelper =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ZHelperFull.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    for i = 0 to bmp.Width-1 do
        for j = 0 to bmp.Height-1 do
            let c = bmp.GetPixel(i,j)
            if c.R = 53uy && c.G = 53uy && c.B = 53uy then
                bmp.SetPixel(i, j, System.Drawing.Color.Black)
            if c.R = 27uy && c.G = 27uy && c.B = 53uy then
                bmp.SetPixel(i, j, System.Drawing.Color.Black)
    bmp
let overworldImage =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("s_map_overworld_strip8.png")
    // 8 maps in here: 1st quest, 2nd quest, 1st quest with mixed secrets, 2nd quest with mixed secrets, and then horizontal-reflected versions of each of those
    new System.Drawing.Bitmap(imageStream)
let zhMapIcons =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("s_icon_overworld_strip39.png")
    new System.Drawing.Bitmap(imageStream)
let zhDungeonIcons =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("s_btn_tr_dungeon_cell_strip3.png")
    new System.Drawing.Bitmap(imageStream)
let zhDungeonNums =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("s_btn_tr_dungeon_num_strip18.png")
    new System.Drawing.Bitmap(imageStream)


let makeVBrect(image) =
    // we need a point of indirection to swap the book and magical_shield icons, so a VisualBrush where we can poke the Visual works
    let vb = new VisualBrush(Visual=image, Opacity=1.0)
    let rect = new System.Windows.Shapes.Rectangle(Height=21., Width=21., Fill=vb)
    rect
let makeObject(bmp) = makeVBrect(BMPtoImage bmp)
// most of these need object identity for logic checks TODO hearts do, others? fix this
// TODO just write my own graphics strip rather than pulling them from zhelper
let boomerang, bow, magic_boomerang, raft, ladder, recorder, wand, red_candle, book, key, silver_arrow, red_ring, boom_book = 
    makeObject boomerang_bmp, makeObject bow_bmp, makeObject magic_boomerang_bmp, makeObject raft_bmp, makeObject ladder_bmp, makeObject recorder_bmp, makeObject wand_bmp, 
    makeObject red_candle_bmp, makeObject book_bmp, makeObject key_bmp, makeObject silver_arrow_bmp, makeObject red_ring_bmp, makeObject boom_book_bmp

let heart_container, power_bracelet, white_sword, ow_key_armos = 
    makeObject heart_container_bmp, makeObject power_bracelet_bmp, makeObject white_sword_bmp, makeObject ow_key_armos_bmp
let copyHeartContainer() =
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    for i = 0 to 20 do
        for j = 0 to 20 do
            bmp.SetPixel(i, j, heart_container_bmp.GetPixel(i,j))
    BMPtoImage bmp

let allItems = [| book; boomerang; bow; power_bracelet; ladder; magic_boomerang; key; raft; recorder; red_candle; red_ring; silver_arrow; wand; white_sword; heart_container |]
let allItemsWithHeartShuffle = 
    [| yield! allItems; for i = 0 to 7 do yield makeVBrect(copyHeartContainer()) |]

let blue_candle = 
    BMPtoImage blue_candle_bmp
let blue_ring = 
    BMPtoImage blue_ring_bmp

let emptyUnfoundTriforces, emptyFoundTriforces , fullTriforces = emptyUnfoundTriforce_bmps |> Array.map BMPtoImage, emptyFoundTriforce_bmps |> Array.map BMPtoImage, fullTriforce_bmps |> Array.map BMPtoImage
let owHeartsSkipped, owHeartsEmpty, owHeartsFull = Array.init 4 (fun _ -> BMPtoImage owHeartSkipped_bmp), Array.init 4 (fun _ -> BMPtoImage owHeartEmpty_bmp), Array.init 4 (fun _ -> BMPtoImage owHeartFull_bmp)
let timelineHeart = 
    let zh = fullZHelper
    let bmp = new System.Drawing.Bitmap(9*3,9*3)
    let xoff,yoff = 1,82  // index into ZHelperFull
    for px = 3 to 10*3-1 do
        for py = 3 to 10*3-1 do
            bmp.SetPixel(px-3, py-3, zh.GetPixel(xoff + px, yoff + py))
    BMPtoImage bmp

let overworldMapBMPs(n) =
    let m = overworldImage
    let tiles = Array2D.zeroCreate 16 8
    for x = 0 to 15 do
        for y = 0 to 7 do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, m.GetPixel(256*n + (x*16*3 + px)/3, (y*11*3 + py)/3))
            tiles.[x,y] <- tile
    tiles

let TRANS_BG = System.Drawing.Color.FromArgb(1, System.Drawing.Color.Black)  // transparent background (will be darkened in program layer)
let uniqueMapIcons, d1bmp, w1bmp =
    let m = zhMapIcons 
    let BLACK = m.GetPixel(( 9*16*3 + 24)/3, (21)/3)
    let tiles = [|
        // levels 1-9
        for i in [2..10] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px/3 >= 5 && px/3 <= 9 && py/3 >= 2 && py/3 <= 8 then
                        let c = m.GetPixel((i*16*3 + px)/3, (py)/3)
                        tile.SetPixel(px, py, if c = BLACK then c else System.Drawing.Color.Yellow)
                    else
                        tile.SetPixel(px, py, TRANS_BG)
            yield tile
        // warps 1-4
        for i in [11..14] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px/3 >= 5 && px/3 <= 9 && py/3 >= 2 && py/3 <= 8 then
                        let c = m.GetPixel(((i-9)*16*3 + px)/3, (py)/3)
                        tile.SetPixel(px, py, if c = BLACK then c else System.Drawing.Color.Aqua)
                    else
                        tile.SetPixel(px, py, TRANS_BG)
            yield tile
        // sword 3, sword 2
        for i in [19..20] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, m.GetPixel((i*16*3 + px)/3, (py)/3))
            yield tile
    |]
    tiles |> Array.map BMPtoImage, tiles.[0], tiles.[9]

let nonUniqueMapIconBMPs = 
    let m = zhMapIcons 
    [|
        // hint shop
        for i in [22] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, m.GetPixel((i*16*3 + px)/3, (py)/3))
            yield tile
        // 3-item shops
        for i in [yield![24..26]; yield![28..31]] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    //tile.SetPixel(px, py, m.GetPixel((i*16*3 + px)/3, (py)/3))
                    if px/3 >= 4 && px/3 <= 10 && py/3 >= 1 && py/3 <= 9 then
                        let c = m.GetPixel((i*16*3 + px + 12)/3, (py)/3)
                        tile.SetPixel(px, py, c)
                    else
                        tile.SetPixel(px, py, TRANS_BG)
            yield tile
        // others
        for i in [yield 15; yield 32; yield 34; yield 38] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, m.GetPixel((i*16*3 + px)/3, (py)/3))
                    if i=38 then
                        if tile.GetPixel(px,py).ToArgb() = System.Drawing.Color.Black.ToArgb() then
                            tile.SetPixel(px,py,System.Drawing.Color.DarkGray)
                        else
                            tile.SetPixel(px,py,System.Drawing.Color.Black)
            yield tile
    |]

let dungeonUnexploredRoomBMP =
    let m = zhDungeonIcons
    let tile = new System.Drawing.Bitmap(13*3, 9*3)
    for px = 0 to 13*3-1 do
        for py = 0 to 9*3-1 do
            tile.SetPixel(px, py, m.GetPixel(px/3, py/3))
    tile

let dungeonExploredRoomBMP =
    let m = zhDungeonIcons
    let tile = new System.Drawing.Bitmap(13*3, 9*3)
    for px = 0 to 13*3-1 do
        for py = 0 to 9*3-1 do
            tile.SetPixel(px, py, m.GetPixel(13+px/3, py/3))
    tile

let dungeonVChuteBMP =
    let tile = dungeonExploredRoomBMP.Clone() :?> System.Drawing.Bitmap 
    let c = tile.GetPixel(0,0)
    for py = 0 to 9*3-1 do
        tile.SetPixel(13, py, c)
        tile.SetPixel(14, py, c)
        tile.SetPixel(15, py, c)
        tile.SetPixel(23, py, c)
        tile.SetPixel(24, py, c)
        tile.SetPixel(25, py, c)
    tile    

let dungeonHChuteBMP =
    let tile = dungeonExploredRoomBMP.Clone() :?> System.Drawing.Bitmap 
    let c = tile.GetPixel(0,0)
    for px = 0 to 13*3-1 do
        tile.SetPixel(px,  9, c)
        tile.SetPixel(px, 10, c)
        tile.SetPixel(px, 11, c)
        tile.SetPixel(px, 15, c)
        tile.SetPixel(px, 16, c)
        tile.SetPixel(px, 17, c)
    tile    

let dungeonTeeBMP =
    let tile = dungeonExploredRoomBMP.Clone() :?> System.Drawing.Bitmap 
    let c = tile.GetPixel(0,0)
    for py = 5*3 to 9*3-1 do
        tile.SetPixel(15, py, c)
        tile.SetPixel(16, py, c)
        tile.SetPixel(17, py, c)
        tile.SetPixel(21, py, c)
        tile.SetPixel(22, py, c)
        tile.SetPixel(23, py, c)
    for px = 4*3 to 9*3-1 do
        for py = 9 to 11 do
            tile.SetPixel(px,  py, c)
    for px in [12;13;14;24;25;26] do
        for py = 12 to 17 do
            tile.SetPixel(px,  py, c)
    tile    

let dungeonTriforceBMP =
    let m = zhDungeonIcons
    let tile = new System.Drawing.Bitmap(13*3, 9*3)
    for px = 0 to 13*3-1 do
        for py = 0 to 9*3-1 do
            tile.SetPixel(px, py, System.Drawing.Color.Yellow)
    tile

let dungeonPrincessBMP =
    let m = zhDungeonIcons
    let tile = new System.Drawing.Bitmap(13*3, 9*3)
    for px = 0 to 13*3-1 do
        for py = 0 to 9*3-1 do
            tile.SetPixel(px, py, System.Drawing.Color.Red)
    tile

let dungeonStartBMP =
    let m = zhDungeonIcons
    let tile = new System.Drawing.Bitmap(13*3, 9*3)
    for px = 0 to 13*3-1 do
        for py = 0 to 9*3-1 do
            if px<3 || px>12*3-1 || py<3 || py>8*3-1 then
                tile.SetPixel(px, py, System.Drawing.Color.LightGray)
            else
                tile.SetPixel(px, py, System.Drawing.Color.Green)
    tile

let dungeonNumberBMPs = 
    let m = zhDungeonNums
    let x = System.Drawing.Color.FromArgb(255, 128, 128, 128)
    let colors = 
        [|
            System.Drawing.Color.Pink 
            System.Drawing.Color.Aqua
            System.Drawing.Color.Orange 
            System.Drawing.Color.FromArgb(0, 140, 0)
            System.Drawing.Color.FromArgb(230, 0, 230) 
            System.Drawing.Color.FromArgb(220, 220, 0)
            System.Drawing.Color.Lime 
            System.Drawing.Color.Brown 
            System.Drawing.Color.Blue
        |]
    [|
        for i = 0 to 8 do
            let tile = dungeonExploredRoomBMP.Clone() :?> System.Drawing.Bitmap 
            for px = 0 to 9*3-1 do
                for py = 0 to 9*3-1 do
                    let c = m.GetPixel((i*9*3+px)/3, py/3)
                    let r = 
                        if c.ToArgb() = x.ToArgb() then
                            colors.[i]
                        else
                            c
                    tile.SetPixel(px+2*3, py, r)
            yield tile
    |]

let ganon,zelda,bomb = 
    BMPtoImage ganon_bmp, BMPtoImage zelda_bmp, BMPtoImage bomb_bmp