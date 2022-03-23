﻿module Graphics

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let mutable theWindow : System.Windows.Window = null
type Win32() =
    [<System.Runtime.InteropServices.DllImport("User32.dll")>]
    static extern bool SetCursorPos(int X, int Y)
    
    static member SetCursor(x,y) = 
        let transformedPoint = 
            if TrackerModel.Options.SmallerAppWindow.Value then 
                Point(x*TrackerModel.Options.SmallerAppWindowScaleFactor,y*TrackerModel.Options.SmallerAppWindowScaleFactor) 
            else Point(x,y)
        let pos = theWindow.PointToScreen(transformedPoint)
        SetCursorPos(int pos.X, int pos.Y) |> ignore

let volumeChanged = new Event<int>()
let soundPlayer = new MediaPlayer()
soundPlayer.Volume <- float TrackerModel.Options.Volume / 300.
soundPlayer.Open(new Uri("confirm_speech.wav", UriKind.Relative))
let PlaySoundForSpeechRecognizedAndUsedToMark() =
    soundPlayer.Position <- TimeSpan(0L)
    soundPlayer.Play()
let soundPlayer2 = new MediaPlayer()
soundPlayer2.Volume <- float TrackerModel.Options.Volume / 300.
soundPlayer2.Open(new Uri("reminder_clink.wav", UriKind.Relative))
let PlaySoundForReminder() =
    soundPlayer2.Position <- TimeSpan(0L)
    soundPlayer2.Play()
volumeChanged.Publish.Add(fun v ->
    soundPlayer.Volume <- float v / 300.
    soundPlayer2.Volume <- float v / 300.
    )


let GetResourceStream(name) = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name)

let unparent(fe:FrameworkElement) =
    if fe.Parent<> null then
        (fe.Parent :?> Panel).Children.Remove(fe)

let canvasAdd(c:Canvas, item, left, top) =
    if item <> null then
        c.Children.Add(item) |> ignore
        Canvas.SetTop(item, top)
        Canvas.SetLeft(item, left)

let gridAdd(g:Grid, x, c, r) =
    g.Children.Add(x) |> ignore
    Grid.SetColumn(x, c)
    Grid.SetRow(x, r)
let makeGrid(nc, nr, cw, rh) =
    let grid = new Grid()
    for i = 0 to nc-1 do
        let w = if cw = -1 then GridLength.Auto else GridLength(float cw)
        grid.ColumnDefinitions.Add(new ColumnDefinition(Width=w))
    for i = 0 to nr-1 do
        let h = if rh = -1 then GridLength.Auto else GridLength(float rh)
        grid.RowDefinitions.Add(new RowDefinition(Height=h))
    grid
let center(e, w, h) =
    let g = makeGrid(1, 1, w, h)
    gridAdd(g, e, 0, 0)
    g
let dock(e, dock) =
    let d = new DockPanel(LastChildFill=false)
    DockPanel.SetDock(e, dock)
    d.Children.Add(e) |> ignore
    d
let makeArrow(targetX, targetY, sourceX, sourceY, brush) =
    let tx,ty = targetX, targetY
    let sx,sy = sourceX, sourceY
    // line from source to target
    let line = new Shapes.Line(X1=sx, Y1=sy, X2=tx, Y2=ty, Stroke=brush, StrokeThickness=3.)
    line.StrokeDashArray <- new DoubleCollection(seq[5.;4.])
    let sq(x) = x*x
    let pct = 1. - 15./sqrt(sq(tx-sx)+sq(ty-sy))   // arrowhead base ideally 15 pixels down the line
    let pct = max pct 0.93                         // but at most 93% towards the target, for small lines
    let ax,ay = (tx-sx)*pct+sx, (ty-sy)*pct+sy
    // differential between target and arrowhead base
    let dx,dy = tx-ax, ty-ay
    // points orthogonal to the line from the base
    let p1x,p1y = ax+dy/2., ay-dx/2.
    let p2x,p2y = ax-dy/2., ay+dx/2.
    // triangle to make arrowhead
    let triangle = new Shapes.Polygon(Fill=brush)
    triangle.Points <- new PointCollection([Point(tx,ty); Point(p1x,p1y); Point(p2x,p2y)])
    line, triangle
let scaleUpCheckBoxBox(cb:CheckBox, scale) =
    cb.LayoutTransform <- new ScaleTransform(scale, scale)
    (cb.Content :?> FrameworkElement).LayoutTransform <- new ScaleTransform(1.0/scale, 1.0/scale)

let almostBlack = new SolidColorBrush(Color.FromRgb(30uy, 30uy, 30uy))
let makeButton(text, fontSizeOpt, fgOpt) =
    let tb = new TextBox(Text=text, IsReadOnly=true, IsHitTestVisible=false, TextAlignment=TextAlignment.Center, BorderThickness=Thickness(0.), Background=almostBlack)
    match fontSizeOpt with | None -> () | Some x -> tb.FontSize <- x
    match fgOpt with | None -> () | Some x -> tb.Foreground <- x
    let button = new Button(Content=tb, HorizontalContentAlignment=HorizontalAlignment.Stretch, VerticalContentAlignment=VerticalAlignment.Stretch)
    button

let makeColor(rgb) =
    let r = (rgb &&& 0xFF0000) / 0x10000
    let g = (rgb &&& 0x00FF00) / 0x100
    let b = (rgb &&& 0x0000FF) / 0x1
    Color.FromRgb(byte r, byte g, byte b)

let isBlackGoodContrast(rgb) =
    // https://gamedev.stackexchange.com/questions/38536/given-a-rgb-color-x-how-to-find-the-most-contrasting-color-y
    let r = (rgb &&& 0xFF0000) / 0x10000
    let g = (rgb &&& 0x00FF00) / 0x100
    let b = (rgb &&& 0x0000FF) / 0x1
    let r = float r / 255.
    let g = float g / 255.
    let b = float b / 255.
    let l = 0.2126 * r*r + 0.7152 * g*g + 0.0722 * b*b
    let use_black = l > 0.5*0.5
    use_black  // else use white

let transformColor(bmp:System.Drawing.Bitmap, f) =
    let r = new System.Drawing.Bitmap(bmp.Width,bmp.Height)
    for px = 0 to bmp.Width-1 do
        for py = 0 to bmp.Height-1 do
            let c = bmp.GetPixel(px,py)
            r.SetPixel(px, py, f c)
    r

let greyscale(bmp:System.Drawing.Bitmap) =
    transformColor(bmp, fun c -> 
        if c.ToArgb() = System.Drawing.Color.Transparent.ToArgb() then
            c
        else
            let avg = (int c.R + int c.G + int c.B) / 5  // not just average, but overall darker
            let avg = if avg = 0 then 0 else avg + 60    // never too dark
            System.Drawing.Color.FromArgb(avg, avg, avg)
        )
let desaturateColor(c:System.Drawing.Color, pct) =
    let f = pct   // 0.60 // desaturate by 60%
    let L = 0.3*float c.R + 0.6*float c.G + 0.1*float c.B
    let newR = float c.R + f * (L - float c.R)
    let newG = float c.G + f * (L - float c.G)
    let newB = float c.B + f * (L - float c.B)
    System.Drawing.Color.FromArgb(int newR, int newG, int newB)
let desaturate(bmp:System.Drawing.Bitmap, pct) = transformColor(bmp, (fun c -> desaturateColor(c,pct)))
let darkenImpl pct (bmp:System.Drawing.Bitmap) =
    let r = new System.Drawing.Bitmap(bmp.Width,bmp.Height)
    for px = 0 to bmp.Width-1 do
        for py = 0 to bmp.Height-1 do
            let c = bmp.GetPixel(px,py)
            let c = System.Drawing.Color.FromArgb(int(float c.R * pct), int(float c.G * pct), int(float c.B * pct))
            r.SetPixel(px, py, c)
    r
let darken(bmp:System.Drawing.Bitmap) =
    darkenImpl 0.5 bmp
let mediaColor(c:System.Drawing.Color) =
    Media.Color.FromArgb(c.A, c.R, c.G, c.B)

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

let OMTW = 48.  // overworld map tile width - at normal aspect ratio, is 48 (16*3)
type TileHighlightRectangle() as this =
    let green = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Lime, 0.50)))
    let yellow = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Yellow, 0.50)))
    let red = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Red, 0.50)))
    let palegreen = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Lime, 0.65)))
    let paleyellow = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Yellow, 0.65)))
    let palered = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Red, 0.65)))
    let shapes = [|
        new Shapes.Rectangle(Width=OMTW,Height=11.*3.,Stroke=Brushes.Lime,StrokeThickness=3.,Opacity=1.0,IsHitTestVisible=false)
        |]
    let Draw(s:Shapes.Rectangle, isPale) =
        s.Opacity <- 1.0
        s.StrokeThickness <- if isPale then 2.0 else 4.0
    do
        this.MakeGreen()
    member _this.MakeRed() = for s in shapes do (s.Stroke <- red; Draw(s,false))
    member _this.MakeYellow() = for s in shapes do (s.Stroke <- yellow; Draw(s,false))
    member _this.MakeBoldGreen() = for s in shapes do (s.Stroke <- green; Draw(s,false); s.StrokeThickness <- 6.0)
    member _this.MakeGreen() = for s in shapes do (s.Stroke <- green; Draw(s,false))
    member _this.MakePaleRed() = for s in shapes do (s.Stroke <- palered; Draw(s,true))
    member _this.MakePaleYellow() = for s in shapes do (s.Stroke <- paleyellow; Draw(s,true))
    member _this.MakePaleGreen() = for s in shapes do (s.Stroke <- palegreen; Draw(s,true))
    member _this.Hide() = for s in shapes do (s.Opacity <- 0.0)
    member _this.Shapes = shapes


// see also
// https://stackoverflow.com/questions/63184765/wpf-left-click-and-drag
// https://stackoverflow.com/questions/12802122/wpf-handle-drag-and-drop-as-well-as-left-click
let setupClickVersusDrag(e:FrameworkElement, onClick, onStartDrag) =  // will only call onClick on 'clicks', will tell you when drag starts, you can call DoDragDrop or not
    let mutable isDragging = false
    let mutable startPoint = None
    e.PreviewMouseDown.Add(fun ea -> 
        startPoint <- Some(ea.GetPosition(null))
        )
    e.PreviewMouseMove.Add(fun ea ->
        if (ea.LeftButton = System.Windows.Input.MouseButtonState.Pressed ||
            ea.MiddleButton = System.Windows.Input.MouseButtonState.Pressed ||
            ea.RightButton = System.Windows.Input.MouseButtonState.Pressed)
                && not isDragging && startPoint.IsSome then
            let pos = ea.GetPosition(null)
            if Math.Abs(pos.X - startPoint.Value.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(pos.Y - startPoint.Value.Y) > SystemParameters.MinimumVerticalDragDistance then
                isDragging <- true
                onStartDrag(ea)
                isDragging <- false
        )
    e.MouseUp.Add(fun ea ->
        if not isDragging then
            if startPoint.IsSome then
                onClick(ea)
            // else e.g. dragged into and released
        startPoint <- None
        )

////////////////////////////////////////////////////

let alphaNumBmp =
    let imageStream = GetResourceStream("alphanumerics3x5.png")
    new System.Drawing.Bitmap(imageStream)
let paintAlphanumerics3x5(ch, color, bmp:System.Drawing.Bitmap, x, y) =  // x and y are 1-pixel coordinates, even though bmp is blown up 3x
    let index =
        match ch with
        | '1' -> 0
        | '2' -> 1
        | '3' -> 2
        | '4' -> 3
        | '5' -> 4
        | '6' -> 5
        | '7' -> 6
        | '8' -> 7
        | '9' -> 8
        | '?' -> 9
        | 'A' -> 10
        | 'B' -> 11
        | 'C' -> 12
        | 'D' -> 13
        | 'E' -> 14
        | 'F' -> 15
        | 'G' -> 16
        | 'H' -> 17
        | _ -> failwith "bad alphanumeric character to paint"
    for i = 0 to 2 do
        for j = 0 to 4 do
            if alphaNumBmp.GetPixel(index*3 + i, j).ToArgb() = System.Drawing.Color.Black.ToArgb() then
                // blow it up 3x
                for dx = 0 to 2 do
                    for dy = 0 to 2 do
                        bmp.SetPixel(3*(x+i)+dx, 3*(y+j)+dy, color)

let alphaNumOnTransparentBmp(ch, color, w:int, h:int, x, y) =
    let r = new System.Drawing.Bitmap(w,h)
    for px = 0 to w-1 do
        for py = 0 to h-1 do
            r.SetPixel(px, py, System.Drawing.Color.Transparent)
    paintAlphanumerics3x5(ch, color, r, x, y)
    r

let openCaveIconBmp = 
    let imageStream = GetResourceStream("open_cave20x20.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    bmp

let (boomerang_bmp, bow_bmp, magic_boomerang_bmp, raft_bmp, ladder_bmp, recorder_bmp, wand_bmp, red_candle_bmp, book_bmp, key_bmp, 
        silver_arrow_bmp, wood_arrow_bmp, red_ring_bmp, magic_shield_bmp, boom_book_bmp, 
        heart_container_bmp, power_bracelet_bmp, white_sword_bmp, ow_key_armos_bmp,
        brown_sword_bmp, magical_sword_bmp, blue_candle_bmp, blue_ring_bmp,
        ganon_bmp, zelda_bmp, bomb_bmp, bow_and_arrow_bmp, bait_bmp, question_marks_bmp, rupee_bmp, basement_stair_bmp) =
    let imageStream = GetResourceStream("icons7x7.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|  
        for i = 0 to bmp.Width/7 - 1 do
            let r = new System.Drawing.Bitmap(7*3,7*3)
            for px = 0 to 7*3-1 do
                for py = 0 to 7*3-1 do
                    let color = bmp.GetPixel(px/3 + i*7, py/3)
                    let color = if color.ToArgb() = System.Drawing.Color.Black.ToArgb() then System.Drawing.Color.Transparent else color
                    r.SetPixel(px, py, color)
            yield r
    |]
    (a.[0], a.[1], a.[2], a.[3], a.[4], a.[5], a.[6], a.[7], a.[8], a.[9],
        a.[10], a.[11], a.[12], a.[13], a.[14], a.[15], a.[16], a.[17], a.[18], a.[19],
        a.[20], a.[21], a.[22], a.[23], a.[24], a.[25], a.[26], a.[27], a.[28], a.[29], a.[30])

let _brightTriforce_bmp, fullOrangeTriforce_bmp, _dullOrangeTriforce_bmp, greyTriforce_bmp, owHeartSkipped_bmp, owHeartEmpty_bmp, owHeartFull_bmp, iconRightArrow_bmp, iconCheckMark_bmp, iconExtras_bmp, iconDisk_bmp = 
    let imageStream = GetResourceStream("icons10x10.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let all = [|
        for i = 0 to bmp.Width/10 - 1 do
            let r = new System.Drawing.Bitmap(10*3,10*3)
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    let color = bmp.GetPixel(px/3 + i*10, py/3)
                    let color = if color.ToArgb() = System.Drawing.Color.White.ToArgb() then System.Drawing.Color.Transparent else color
                    r.SetPixel(px, py, color)
            yield r
        |]
    transformColor(all.[1], (fun c -> if c.ToArgb() <> System.Drawing.Color.Transparent.ToArgb() then System.Drawing.Color.LightGray else c)), 
        all.[1],
        transformColor(all.[1], (fun c -> if c.ToArgb() <> System.Drawing.Color.Transparent.ToArgb() then desaturateColor(c, 0.25) else c)), 
        all.[0], all.[2], all.[3], all.[4], all.[5], all.[6], all.[7], all.[8]
let UNFOUND_NUMERAL_COLOR = System.Drawing.Color.FromArgb(0x77,0x77,0x99)
let FOUND_NUMERAL_COLOR = System.Drawing.Color.White
let emptyUnfoundNumberedTriforce_bmps, emptyUnfoundLetteredTriforce_bmps = 
    let a = [|
        for ch in ['1'; 'A'] do
            yield [|
            for i = 0 to 7 do
                let bmp = greyTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), UNFOUND_NUMERAL_COLOR, bmp, 4, 4)
                yield bmp
            |] |]
    a.[0], a.[1]
let emptyFoundNumberedTriforce_bmps, emptyFoundLetteredTriforce_bmps = 
    let a = [|
        for ch in ['1'; 'A'] do
            yield [|
            for i = 0 to 7 do
                let bmp = greyTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), FOUND_NUMERAL_COLOR, bmp, 4, 4)
                yield bmp
            |] |]
    a.[0], a.[1]
let fullNumberedUnfoundTriforce_bmps, fullNumberedFoundTriforce_bmps, fullLetteredUnfoundTriforce_bmps, fullLetteredFoundTriforce_bmps =
    let a = [|
        for ch in ['1'; 'A'] do
            yield [|
            for i = 0 to 7 do
                let bmp = fullOrangeTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), UNFOUND_NUMERAL_COLOR, bmp, 4, 4)
                yield bmp
            |]
            yield [|
            for i = 0 to 7 do
                let bmp = fullOrangeTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), FOUND_NUMERAL_COLOR, bmp, 4, 4)
                yield bmp
            |]
        |]
    a.[0], a.[1], a.[2], a.[3]
let unfoundL9_bmp,foundL9_bmp =
    let unfoundTriforceColor = emptyUnfoundNumberedTriforce_bmps.[0].GetPixel(15, 28)
    let u = new System.Drawing.Bitmap(10*3,10*3)
    let f = new System.Drawing.Bitmap(10*3,10*3)
    // make a rectangle 'shield' to draw the 9 on, so that a hint halo does not wash out the '9'
    for i = 3*3 to 8*3-1 do
        for j = 3*3 to 10*3-1 do
            u.SetPixel(i,j,unfoundTriforceColor)
            f.SetPixel(i,j,unfoundTriforceColor)
    paintAlphanumerics3x5('9', UNFOUND_NUMERAL_COLOR, u, 4, 4)
    paintAlphanumerics3x5('9', FOUND_NUMERAL_COLOR, f, 4, 4)
    u, f

let fairy_bmp =
    let imageStream = GetResourceStream("icons8x16.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let r = new System.Drawing.Bitmap(16,32)
    for x = 0 to 15 do
        for y = 0 to 31 do
            let c = bmp.GetPixel(x/2, y/2)
            if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                r.SetPixel(x, y, c)
    r

let dungeonRoomBmpPairs =
    let imageStream = GetResourceStream("new_icons13x9.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|  
        for i = 0 to bmp.Width/13 - 1 do
            let cr = new System.Drawing.Bitmap(13*3,9*3)
            let ur = new System.Drawing.Bitmap(13*3,9*3)
            for px = 0 to 13*3-1 do
                for py = 0 to 9*3-1 do
                    ur.SetPixel(px, py, bmp.GetPixel(px/3 + i*13,   py/3))
                    cr.SetPixel(px, py, bmp.GetPixel(px/3 + i*13, 9+py/3))
            yield (ur,cr)
    |]
    a
let dungeonRoomFloorDrops, dungeonRoomMonsters =
    let imageStream = GetResourceStream("icons3x3.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|  
        for i = 0 to bmp.Width/3 - 1 do
            let r = new System.Drawing.Bitmap(5*3,5*3)
            let b = new System.Drawing.Bitmap(8*3,8*3)
            // black border around them
            for x = 0 to 14 do
                for y = 0 to 14 do
                    r.SetPixel(x,y,System.Drawing.Color.Black)
            for x = 0 to 23 do
                for y = 0 to 23 do
                    b.SetPixel(x,y,System.Drawing.Color.Black)
            // the icon in the middle
            for px = 0 to 3*3-1 do
                for py = 0 to 3*3-1 do
                    r.SetPixel(px+3, py+3, bmp.GetPixel(px/3 + i*3,   py/3))
                    b.SetPixel(2*px+3, 2*py+3, bmp.GetPixel(px/3 + i*3,   py/3))
                    b.SetPixel(2*px+1+3, 2*py+3, bmp.GetPixel(px/3 + i*3,   py/3))
                    b.SetPixel(2*px+3, 2*py+1+3, bmp.GetPixel(px/3 + i*3,   py/3))
                    b.SetPixel(2*px+1+3, 2*py+1+3, bmp.GetPixel(px/3 + i*3,   py/3))
            yield r, b
    |]
    a.[0..7], a.[8..]

let overworldImage =
    let files = [|
//        "s_map_overworld_strip8.png"
        "s_map_overworld_vanilla_strip8.png"
//        "s_map_overworld_zones_strip8.png"
        |]
    let file = files.[(new System.Random()).Next(files.Length)]
//    printfn "selecting overworld file %s" file
    let imageStream = GetResourceStream(file)
    new System.Drawing.Bitmap(imageStream)
let zhMapIcons =
    let imageStream = GetResourceStream("s_icon_overworld_strip39.png")
    new System.Drawing.Bitmap(imageStream)
let zhDungeonIcons =
    let imageStream = GetResourceStream("s_btn_tr_dungeon_cell_strip3.png")
    new System.Drawing.Bitmap(imageStream)
let zhDungeonNums =
    let imageStream = GetResourceStream("s_btn_tr_dungeon_num_strip18.png")
    new System.Drawing.Bitmap(imageStream)


let allItemBMPs = [| book_bmp; boomerang_bmp; bow_bmp; power_bracelet_bmp; ladder_bmp; magic_boomerang_bmp; key_bmp; raft_bmp; recorder_bmp; red_candle_bmp; red_ring_bmp; silver_arrow_bmp; wand_bmp; white_sword_bmp |]
let allItemBMPsWithHeartShuffle = [| yield! allItemBMPs; for _i = 0 to 8 do yield heart_container_bmp |]

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
let itemBackgroundColor = System.Drawing.Color.FromArgb(0xEF,0x83,0)
let itemsBMP = 
    let imageStream = GetResourceStream("icons3x7.png")
    new System.Drawing.Bitmap(imageStream)

let genericDungeonInterior_bmp =
    let bmp = new System.Drawing.Bitmap(5*3,9*3)
    for px = 0 to 5*3-1 do
        for py = 0 to 9*3-1 do
            bmp.SetPixel(px, py, System.Drawing.Color.Yellow)
    paintAlphanumerics3x5('?', System.Drawing.Color.Black, bmp, 1, 2)
    bmp

// each overworld map tile may have multiple icons that can represent it (e.g. dungeon 1 versus dungeon A)
// we store a table, where the array index is the mapSquareChoiceDomain index of the general entry type, and the value there is a list of all possible icons
// MapStateProxy will eventually be responsible for 'decoding' the current tracker state into the appropriate icon
let theInteriorBmpTable = Array.init (TrackerModel.dummyOverworldTiles.Length) (fun _ -> ResizeArray())
do
    let imageStream = GetResourceStream("ow_icons5x9.png")
    let interiorIconStrip = new System.Drawing.Bitmap(imageStream)
    let darkxbmp = new System.Drawing.Bitmap(5*3,9*3)
    for px = 0 to 5*3-1 do
        for py = 0 to 9*3-1 do
            darkxbmp.SetPixel(px, py, System.Drawing.Color.Black)
    let getInteriorIconFromStrip(i) = 
        let bmp = new System.Drawing.Bitmap(5*3,9*3)
        for px = 0 to 5*3-1 do
            for py = 0 to 9*3-1 do
                bmp.SetPixel(px, py, interiorIconStrip.GetPixel(i*5+px/3, py/3))
        bmp
    // 0-8  dungeons: 4 varieties (numbered yellow, numbered green, lettered yellow, lettered green)
    for labels in ["123456789";"ABCDEFGH9"] do
        for color in [System.Drawing.Color.Yellow; System.Drawing.Color.Lime] do
            labels |> Seq.iteri (fun i ch ->
                let bmp = new System.Drawing.Bitmap(5*3,9*3)
                for px = 0 to 5*3-1 do
                    for py = 0 to 9*3-1 do
                        bmp.SetPixel(px, py, color)
                paintAlphanumerics3x5(ch, System.Drawing.Color.Black, bmp, 1, 2)
                theInteriorBmpTable.[i].Add(bmp)
                )
    // 9-12  any roads
    "1234" |> Seq.iteri (fun i ch ->
        let bmp = new System.Drawing.Bitmap(5*3,9*3)
        for px = 0 to 5*3-1 do
            for py = 0 to 9*3-1 do
                bmp.SetPixel(px, py, System.Drawing.Color.Orchid)
        paintAlphanumerics3x5(ch, System.Drawing.Color.Black, bmp, 1, 2)
        theInteriorBmpTable.[i+9].Add(bmp)
        )
    // 13  sword3
    theInteriorBmpTable.[13].Add(getInteriorIconFromStrip(0))
    // 14  sword2
    theInteriorBmpTable.[14].Add(getInteriorIconFromStrip(1))
    // 15  sword1
    theInteriorBmpTable.[15].Add(getInteriorIconFromStrip(2))
    theInteriorBmpTable.[15].Add(getInteriorIconFromStrip(2) |> darken)
    // 16-23  item shops (as single-item icons)
    for i = 0 to TrackerModel.MapSquareChoiceDomainHelper.NUM_ITEMS-1 do
        let bmp = new System.Drawing.Bitmap(5*3,9*3)
        for px = 0 to 5*3-1 do
            for py = 0 to 9*3-1 do
                bmp.SetPixel(px, py, itemBackgroundColor)
                if px/3 >= 1 && px/3 <= 3 && py/3 >= 1 && py/3 <= 7 then
                    let c = itemsBMP.GetPixel(i*3 + px/3-1, py/3-1)
                    if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                        bmp.SetPixel(px, py, c)
        theInteriorBmpTable.[i+16].Add(bmp)
    // 24  unknown money secret
    theInteriorBmpTable.[24].Add(getInteriorIconFromStrip(11))
    // 25  large secret
    theInteriorBmpTable.[25].Add(getInteriorIconFromStrip(8))
    theInteriorBmpTable.[25].Add(getInteriorIconFromStrip(8) |> darken)
    // 26  medium secret
    theInteriorBmpTable.[26].Add(getInteriorIconFromStrip(6))
    theInteriorBmpTable.[26].Add(getInteriorIconFromStrip(6) |> darken)
    // 27  small secret
    theInteriorBmpTable.[27].Add(getInteriorIconFromStrip(9))
    theInteriorBmpTable.[27].Add(getInteriorIconFromStrip(9) |> darken)
    // 28  door repair charge
    theInteriorBmpTable.[28].Add(getInteriorIconFromStrip(12))
    theInteriorBmpTable.[28].Add(getInteriorIconFromStrip(12) |> darkenImpl 0.7)
    // 29  money making game
    theInteriorBmpTable.[29].Add(getInteriorIconFromStrip(10))
    // 30  letter
    theInteriorBmpTable.[30].Add(getInteriorIconFromStrip(13))
    theInteriorBmpTable.[30].Add(getInteriorIconFromStrip(13) |> darkenImpl 0.7)
    // 31  armos
    theInteriorBmpTable.[31].Add(getInteriorIconFromStrip(7))
    // 32  hint shop
    theInteriorBmpTable.[32].Add(getInteriorIconFromStrip(3))
    theInteriorBmpTable.[32].Add(getInteriorIconFromStrip(3) |> darkenImpl 0.7)
    // 33  take any
    theInteriorBmpTable.[33].Add(getInteriorIconFromStrip(4))
    theInteriorBmpTable.[33].Add(getInteriorIconFromStrip(4) |> darken)
    // 34  potion shop
    theInteriorBmpTable.[34].Add(getInteriorIconFromStrip(5))
    // 35  'X'
    theInteriorBmpTable.[35].Add(darkxbmp)
// full tiles just have interior bmp in the center and transparent pixels all around (except for the final 'X' one)
let theFullTileBmpTable = Array.init theInteriorBmpTable.Length (fun _ -> ResizeArray())
do
    for i = 0 to theInteriorBmpTable.Length-1 do
        for interiorBmp in theInteriorBmpTable.[i] do
            let fullTileBmp = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px>=5*3 && px<10*3 && py>=1*3 && py<10*3 then 
                        fullTileBmp.SetPixel(px, py, interiorBmp.GetPixel(px-5*3, py-1*3))
                    else
                        fullTileBmp.SetPixel(px, py, if i=theInteriorBmpTable.Length-1 then System.Drawing.Color.Black else TRANS_BG)
            theFullTileBmpTable.[i].Add(fullTileBmp)
            
let linkFaceForward_bmp,linkRunRight_bmp,linkFaceRight_bmp,linkGotTheThing_bmp =
    let imageStream = GetResourceStream("link_icons.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|
        for i = 0 to 3 do
            let r = new System.Drawing.Bitmap(16, 16)
            for x = 0 to 15 do
                for y = 0 to 15 do
                    let color = bmp.GetPixel(16*i+x, y)
                    let color = if color.ToArgb() = System.Drawing.Color.Black.ToArgb() then System.Drawing.Color.Transparent else color
                    r.SetPixel(x, y, color)
            yield r
        |]
    a.[0], a.[1], a.[2], a.[3]
    
let loadBMP(filename) = 
    let imageStream = GetResourceStream(filename)
    let bmp = new System.Drawing.Bitmap(imageStream)
    bmp
let mouseIconButtonColorsBMP = loadBMP("mouse-icon-button-colors.png")
let mouseIconButtonColors2BMP = loadBMP("mouse-icon-button-colors-2.png")


let takeAnyPotionBMP = loadBMP("take-any-potion.png")
let takeAnyCandleBMP = loadBMP("take-any-candle.png")
let takeAnyHeartBMP = loadBMP("take-any-heart.png")
let takeAnyLeaveBMP = loadBMP("take-any-leave.png")
let takeThisWoodSwordBMP = loadBMP("take-this-wood-sword.png")
let takeThisCandleBMP = loadBMP("take-this-candle.png")
let takeThisLeaveBMP = loadBMP("take-this-leave.png")

(*
code for clipping screenshots to same size

let clipTakeAny(bmp:System.Drawing.Bitmap) =
    let r = new System.Drawing.Bitmap(672, 448)
    for x = 0 to 671 do
        for y = 0 to 447 do
            r.SetPixel(x,y,bmp.GetPixel(x+48, y+45))
    r
let takeAnyPotionBMP = loadBMP("take-any-potion.png") |> clipTakeAny
let takeAnyCandleBMP = loadBMP("take-any-candle.png") |> clipTakeAny
let takeAnyHeartBMP = loadBMP("take-any-heart.png") |> clipTakeAny
let takeAnyLeaveBMP = loadBMP("take-any-leave.png") |> clipTakeAny
let takeThisWoodSwordBMP = loadBMP("take-this-wood-sword.png") |> clipTakeAny
let takeThisCandleBMP = loadBMP("take-this-candle.png") |> clipTakeAny
let takeThisLeaveBMP = loadBMP("take-this-leave.png") |> clipTakeAny

do
    takeAnyPotionBMP.Save("takeAnyPotionBMP.png")
    takeAnyCandleBMP.Save("takeAnyCandleBMP.png")
    takeAnyHeartBMP.Save("takeAnyHeartBMP.png")
    takeAnyLeaveBMP.Save("takeAnyLeaveBMP.png")
    takeThisWoodSwordBMP.Save("takeThisWoodSwordBMP.png")
    takeThisCandleBMP.Save("takeThisCandleBMP.png")
    takeThisLeaveBMP.Save("takeThisLeaveBMP.png")
*)

let overworldCommonestFloorColorBrush = new SolidColorBrush(Color.FromRgb(204uy,176uy,136uy))
(*
do
    let imageStream = GetResourceStream("icons3x7.png")
    let bmp37 = new System.Drawing.Bitmap(imageStream)
    let imageStream = GetResourceStream("icons7x7.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let newBmp = new System.Drawing.Bitmap(bmp.Width+7, bmp.Height)
    for i = 0 to bmp.Width-1 do
        for j = 0 to bmp.Height-1 do
            newBmp.SetPixel(i,j,bmp.GetPixel(i,j))
    for i = 0 to 6 do
        for j = 0 to 6 do
            newBmp.SetPixel(bmp.Width+i, j, Drawing.Color.Transparent)
    for i = 0 to 2 do
        for j = 0 to 6 do
            newBmp.SetPixel(bmp.Width+i+2, j, bmp37.GetPixel(3*5+i,j))
    newBmp.Save("""C:\Users\Admin1\Source\Repos\Zelda1RandoTools\Z1R_Tracker\Z1R_WPF\tmp.png""")

do
    let imageStream = GetResourceStream("icons7x7.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let newBmp = new System.Drawing.Bitmap(bmp.Width, bmp.Height)
    for i = 0 to bmp.Width-1 do
        for j = 0 to bmp.Height-1 do
            let c = bmp.GetPixel(i,j)
            if c.A <> 0xFFuy then
                newBmp.SetPixel(i,j,System.Drawing.Color.Black)
            else
                newBmp.SetPixel(i,j,c)
    newBmp.Save("""C:\Users\Admin1\Source\Repos\Zelda1RandoTools\Z1R_Tracker\Z1R_WPF\tmp.png""")
*)

let swordLevelToBmp(swordLevel) =
    match swordLevel with
    | 0 -> greyscale magical_sword_bmp
    | 1 -> brown_sword_bmp
    | 2 -> white_sword_bmp
    | 3 -> magical_sword_bmp
    | _ -> failwith "bad SwordLevel"
let ringLevelToBmp(ringLevel) =
    match ringLevel with
    | 0 -> greyscale red_ring_bmp
    | 1 -> blue_ring_bmp
    | 2 -> red_ring_bmp
    | _ -> failwith "bad RingLevel"

let blockerCurrentBMP(current) =
    let gsc = new GradientStopCollection([new GradientStop(Color.FromArgb(255uy, 0uy, 180uy, 0uy), 0.)
                                          new GradientStop(Color.FromArgb(255uy, 40uy, 40uy, 40uy), 0.3)
                                          new GradientStop(Color.FromArgb(255uy, 40uy, 40uy, 40uy), 0.7)
                                          new GradientStop(Color.FromArgb(255uy, 180uy, 0uy, 0uy), 1.0)
                                         ])
    let maybeBG = new LinearGradientBrush(gsc, Point(0.,0.), Point(1.,1.))
    let innerc = new Canvas(Width=24., Height=24., Background=Brushes.Transparent, IsHitTestVisible=false)  // just has item drawn on it, not the box
    innerc.Children.Clear()
    innerc.Background <- match current with
                            | TrackerModel.DungeonBlocker.MAYBE_LADDER
                            | TrackerModel.DungeonBlocker.MAYBE_BAIT
                            | TrackerModel.DungeonBlocker.MAYBE_BOMB
                            | TrackerModel.DungeonBlocker.MAYBE_BOW_AND_ARROW
                            | TrackerModel.DungeonBlocker.MAYBE_KEY
                            | TrackerModel.DungeonBlocker.MAYBE_MONEY
                            | TrackerModel.DungeonBlocker.MAYBE_RECORDER                            
                                -> maybeBG :> Brush
                            | _ -> Brushes.Black :> Brush
    let bmp = 
        match current with
        | TrackerModel.DungeonBlocker.COMBAT -> white_sword_bmp
        | TrackerModel.DungeonBlocker.BOW_AND_ARROW | TrackerModel.DungeonBlocker.MAYBE_BOW_AND_ARROW -> bow_and_arrow_bmp
        | TrackerModel.DungeonBlocker.RECORDER | TrackerModel.DungeonBlocker.MAYBE_RECORDER -> recorder_bmp
        | TrackerModel.DungeonBlocker.LADDER | TrackerModel.DungeonBlocker.MAYBE_LADDER -> ladder_bmp
        | TrackerModel.DungeonBlocker.BAIT | TrackerModel.DungeonBlocker.MAYBE_BAIT -> bait_bmp
        | TrackerModel.DungeonBlocker.KEY | TrackerModel.DungeonBlocker.MAYBE_KEY -> key_bmp
        | TrackerModel.DungeonBlocker.BOMB | TrackerModel.DungeonBlocker.MAYBE_BOMB -> bomb_bmp
        | TrackerModel.DungeonBlocker.MONEY | TrackerModel.DungeonBlocker.MAYBE_MONEY -> rupee_bmp
        | TrackerModel.DungeonBlocker.NOTHING -> null
    if bmp <> null then
        let image = BMPtoImage(bmp)
        image.IsHitTestVisible <- false
        canvasAdd(innerc, image, 1., 1.)
    innerc

let WarpMouseCursorTo(pos:Point) =
    Win32.SetCursor(pos.X, pos.Y)
    PlaySoundForSpeechRecognizedAndUsedToMark()
