using Godot;

namespace NesTemplate.Sample;

public static class PixelArtGenerator
{
    private static readonly Color Transparent = new(0f, 0f, 0f, 0f);
    private static readonly Color Index1Dark = new(0.30f, 0.30f, 0.30f, 1f);
    private static readonly Color Index2Mid = new(0.66f, 0.66f, 0.66f, 1f);
    private static readonly Color Index3Light = new(1.00f, 1.00f, 1.00f, 1f);

    // 16x16 Hero Sprite ('.' = 0, '1' = Dark, '2' = Mid, '3' = Highlight)
    private static readonly string[] HeroPattern =
    {
        "....11111111....",
        "...1222222221...",
        "..122333333221..",
        "..123133331321..",
        "..123133331321..",
        "..123333333321..",
        "...1123333211...",
        "..112222222211..",
        ".12212233221221.",
        ".12212233221221.",
        ".11112222221111.",
        "....12211221....",
        "...1221..1221...",
        "...1221..1221...",
        "..11111..11111..",
        "..11111..11111.."
    };

    // 16x16 Patrol Bot Sprite
    private static readonly string[] BotPattern =
    {
        "......1111......",
        "....11222211....",
        "...1223333221...",
        "..122312213221..",
        ".12231122113221.",
        ".12233333333221.",
        ".12222222222221.",
        ".11122122122111.",
        "..111111111111..",
        "...1221111221...",
        "..1221....1221..",
        "..121......121..",
        ".1121......1211.",
        ".1111......1111.",
        "................",
        "................"
    };

    // 16x16 Classic NES Brick Tile
    private static readonly string[] BrickPattern =
    {
        "3333333133333331",
        "3222222132222221",
        "3222222132222221",
        "1111111111111111",
        "3331333333313333",
        "2221322222213222",
        "2221322222213222",
        "1111111111111111",
        "3333333133333331",
        "3222222132222221",
        "3222222132222221",
        "1111111111111111",
        "3331333333313333",
        "2221322222213222",
        "2221322222213222",
        "1111111111111111"
    };

    public static ImageTexture CreateHeroTexture() => BuildTexture(HeroPattern);
    public static ImageTexture CreateBotTexture() => BuildTexture(BotPattern);
    public static ImageTexture CreateBrickTexture() => BuildTexture(BrickPattern);

    private static ImageTexture BuildTexture(string[] rows)
    {
        Image img = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        for (int y = 0; y < 16; y++)
        {
            string row = rows[y];
            for (int x = 0; x < 16; x++)
            {
                char c = row[x];
                Color pixel = c switch
                {
                    '1' => Index1Dark,
                    '2' => Index2Mid,
                    '3' => Index3Light,
                    _ => Transparent
                };
                img.SetPixel(x, y, pixel);
            }
        }
        return ImageTexture.CreateFromImage(img);
    }
}
