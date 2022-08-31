using System.Text;
using System.Reflection;
using System.Collections.Immutable;
using System.CommandLine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

const char
    ANSI_ESC = '\x1b',
    UPPER_HALF_BLOCK = '\x2580';

ImmutableDictionary<string, IResampler> resamplerMap = typeof(KnownResamplers)
    .GetProperties(BindingFlags.Public | BindingFlags.Static)
    .ToImmutableDictionary(
        prop => prop.Name,
        prop => (IResampler)prop.GetValue(null)!,
        StringComparer.OrdinalIgnoreCase
    );

Argument<FileInfo> fileArg = new("file", "The file path to the image.");
fileArg = fileArg.ExistingOnly();

Option<Size> sizeOpt = new(
    new[] { "-s", "--size" },
    (arg) =>
    {
        string[] split = arg.Tokens[0].Value.Split('x');
        if (split.Length != 2)
        {
            arg.ErrorMessage = "Size must be of the format: [width]x[height].";
            return default;
        }

        int width = 0, height = 0;
        string strWidth = split[0], strHeight = split[1];

        if (!string.IsNullOrWhiteSpace(strWidth) &&
            (!int.TryParse(strWidth, out width) || width < 0))
        {
            arg.ErrorMessage = $"Invalid width: {strWidth}.";
            return default;
        }

        if (!string.IsNullOrWhiteSpace(strHeight) &&
            (!int.TryParse(strHeight, out height) || height < 0))
        {
            arg.ErrorMessage = $"Invalid height: {strHeight}.";
            return default;
        }

        if (width == 0 && height == 0)
        {
            arg.ErrorMessage = $"A width or height must be specified for size.";
            return default;
        }

        return new(width, height);
    },
    description:
        "The size of the output image in the format: [width]x[height]."
        + " The width or height may be left out, in which case it will be"
        + " calculated to preserve the aspect ratio of the original image."
);

Option<string> resamplerOpt = new(
    new[] { "-r", "--resampler" },
    () => nameof(KnownResamplers.NearestNeighbor),
    "The resampler to use when resizing the image."
);
resamplerOpt = resamplerOpt.FromAmong(resamplerMap.Keys.ToArray());

Option<int> frameOpt = new(
    new[] { "-f", "--frame" },
    () => 0,
    "Which frame number to output."
);

RootCommand root = new("Converts an image to ANSI art.");
root.AddArgument(fileArg);
root.AddOption(sizeOpt);
root.AddOption(resamplerOpt);
root.AddOption(frameOpt);

root.SetHandler(
    (file, size, resamplerName, frame) =>
    {
        // Load the image from the specified path
        Image<Rgba32> image = Image.Load<Rgba32>(file.FullName);

        // Resize the image if width or height is specified
        if (size.Width > 0 || size.Height > 0)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = size,
                Sampler = resamplerMap[resamplerName]
            }));
        }

        ImageFrame<Rgba32> imageFrame = image.Frames[frame];

        StringBuilder sb = new();

        // Iterate through pixel rows
        // Each cell displays 2 pixels:
        // - the foreground color for the top pixel (using the upper half block unicode character)
        // - the background color for the bottom pixel
        for (int  y = 0; y < imageFrame.Height; y += 2)
        {
            // Iterate through pixel columns
            for (int x = 0; x < imageFrame.Width; x++)
            {
                Rgba32 c;

                // Begin format escape sequence
                sb.Append($"{ANSI_ESC}[");
                // Check if this row has a bottom pixel row
                if ((y + 1) < imageFrame.Height)
                {
                    // Set background color (bottom pixel)
                    c = imageFrame[x, y + 1];
                    sb.Append($"48;2;{c.R};{c.G};{c.B};");
                }
                else
                {
                    // Reset background to default
                    sb.Append($"49;");
                }
                // Set foreground color (top pixel), end escape sequence and output the upper half block
                c = imageFrame[x, y];
                sb.Append($"38;2;{c.R};{c.G};{c.B}m");
                sb.Append($"{UPPER_HALF_BLOCK}");
            }
            // Reset formatting
            sb.AppendLine($"{ANSI_ESC}[0m");
        }

        Console.Write(sb.ToString());
    },
    fileArg, sizeOpt, resamplerOpt, frameOpt
);

Command getFrameCountCmd = new("get-frame-count", "Get the number of frames contained within the image.");
getFrameCountCmd.AddArgument(fileArg);
getFrameCountCmd.SetHandler(file => Console.WriteLine(Image.Load(file.FullName).Frames.Count), fileArg);

root.AddCommand(getFrameCountCmd);

await root.InvokeAsync(args);