namespace Wysg.Musm.MFCUIA.Abstractions;

public interface IListViewAdapter
{
    int Count();
    string GetText(int row, int col = 0);
    int GetSelectedIndex();
    string[] GetSelectedRow(params int[] columns);
}
