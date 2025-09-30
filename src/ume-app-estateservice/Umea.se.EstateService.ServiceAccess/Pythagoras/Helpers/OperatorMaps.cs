using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Helpers;

public static class OperatorMaps
{
    private static readonly Dictionary<Op, string> _prefix = new()
    {
        [Op.Eq] = "EQ:",
        [Op.Ne] = "NE:",
        [Op.Gt] = "GT:",
        [Op.Ge] = "GE:",
        [Op.Lt] = "LT:",
        [Op.Le] = "LE:",
        [Op.LikeExact] = "LIKEEX:",
        [Op.LikeAnywhere] = "LIKEAW:",
        [Op.LikeStarts] = "LIKEST:",
        [Op.LikeEnds] = "LIKEEN:",
        [Op.ILikeExact] = "ILIKEEX:",
        [Op.ILikeAnywhere] = "ILIKEAW:",
        [Op.ILikeStarts] = "ILIKEST:",
        [Op.ILikeEnds] = "ILIKEEN:",
    };

    public static string ToPrefix(Op op) => _prefix[op];

    public static Op FromStringOrPrefix(string op)
    {
        string u = op.Trim().ToUpperInvariant().TrimEnd(':');
        if (u.Length == 0)
        {
            throw new ArgumentException("Operator cannot be empty.", nameof(op));
        }
        return u switch
        {
            "EQ" or "==" or "=" => Op.Eq,
            "NE" or "!=" => Op.Ne,
            "GT" or ">" => Op.Gt,
            "GE" or ">=" => Op.Ge,
            "LT" or "<" => Op.Lt,
            "LE" or "<=" => Op.Le,
            "LIKEEX" => Op.LikeExact,
            "LIKEAW" => Op.LikeAnywhere,
            "LIKEST" => Op.LikeStarts,
            "LIKEEN" => Op.LikeEnds,
            "ILIKEEX" => Op.ILikeExact,
            "ILIKEAW" => Op.ILikeAnywhere,
            "ILIKEST" => Op.ILikeStarts,
            "ILIKEEN" => Op.ILikeEnds,
            _ => throw new ArgumentException($"Unknown or unsupported operator '{u}'.", nameof(op))
        };
    }
}
