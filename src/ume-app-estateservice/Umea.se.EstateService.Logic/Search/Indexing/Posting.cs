namespace Umea.se.EstateService.Logic.Search.Indexing;

internal sealed class Posting(int docId, Field field, int position)
{
    public int DocId = docId;            // internal int id
    public Field Field = field;          // field the term occurs in
    public List<int> Positions = [position];  // token positions within that field
}
