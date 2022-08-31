# Ansia
CLI tool that converts an image to ANSI art which can be displayed in a terminal.

![image](https://user-images.githubusercontent.com/109945998/187438695-d97bbde5-63f1-41df-8935-4ab1ca8d4ec0.png)

[Image by ReffPixels](https://commons.wikimedia.org/wiki/File:Pixel_art_portrait_of_a_cat.png)

## Usage

Display an image at its original resolution.

```bash
ansia image.png
```

Display an image resized to the specified resolution of 32 by 32 pixels.

```bash
ansia image.png -s 32x32
```

Display an image resized to the specified width of 32 pixels.\
The height will be calculated to maintain the aspect ratio of the original image

```bash
ansia image.png -s 32x
```

Output the converted image to a file then display its contents.

```bash
ansia fish.png -s 200x > fish
cat fish
```

![image](https://user-images.githubusercontent.com/109945998/187431586-c0725dce-3c3d-4d14-b421-8e3977389193.png)

[Image by Duane Raver](https://commons.wikimedia.org/wiki/File:Ameiurus_melas_by_Duane_Raver.png)
