namespace Umea.se.EstateService.Logic.Search.Indexing;

internal sealed class Posting
{
    public int DocId;            // internal int id
    public Field Field;          // field the term occurs in
    public List<int> Positions;  // token positions within that field

    public Posting(int docId, Field field, int position)
    {
        DocId = docId;
        Field = field;
        Positions = new List<int> { position };
    }
}
