namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api.Query;

public enum FieldTarget { Parameter, Attribute }

public enum Op
{
    Eq, Ne, Gt, Ge, Lt, Le,
    LikeExact, LikeAnywhere, LikeStarts, LikeEnds,
    ILikeExact, ILikeAnywhere, ILikeStarts, ILikeEnds
}