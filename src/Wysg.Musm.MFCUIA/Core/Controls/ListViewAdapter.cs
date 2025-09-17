using System;
using System.Runtime.InteropServices;
using Wysg.Musm.MFCUIA.Abstractions;

namespace Wysg.Musm.MFCUIA.Core.Controls;

internal static class ComCtl32
{
    public const int LVM_FIRST = 0x1000;
    public const int LVM_GETITEMCOUNT = LVM_FIRST + 4;
    public const int LVM_GETITEMTEXTW = LVM_FIRST + 115;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LVITEM
    {
        public uint mask;
        public int iItem;
        public int iSubItem;
        public uint state;
        public uint stateMask;
        public IntPtr pszText;
        public int cchTextMax;
        public int iImage;
        public IntPtr lParam;
        public int iIndent;
        public int iGroupId;
        public uint cColumns;
        public IntPtr puColumns;
        public IntPtr piColFmt;
        public int iGroup;
    }
}

internal static class User32ListView
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}

public sealed class ListViewAdapter : IListViewAdapter
{
    private readonly IntPtr _hwnd;
    public ListViewAdapter(IntPtr hwnd) => _hwnd = hwnd;

    public int Count() => (int)User32ListView.SendMessage(_hwnd, ComCtl32.LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);

    public string GetText(int row, int col = 0)
    {
        const int BUF = 1024;
        var buf = Marshal.AllocHGlobal(BUF * 2);
        try
        {
            var item = new ComCtl32.LVITEM { iItem = row, iSubItem = col, pszText = buf, cchTextMax = BUF };
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<ComCtl32.LVITEM>());
            try
            {
                Marshal.StructureToPtr(item, ptr, false);
                _ = User32ListView.SendMessage(_hwnd, ComCtl32.LVM_GETITEMTEXTW, (IntPtr)row, ptr);
                return Marshal.PtrToStringUni(buf) ?? string.Empty;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    public int GetSelectedIndex()
    {
        const int LVM_GETNEXTITEM = ComCtl32.LVM_FIRST + 12;
        const int LVNI_SELECTED = 0x0002;
        return (int)User32ListView.SendMessage(_hwnd, LVM_GETNEXTITEM, (IntPtr)(-1), (IntPtr)LVNI_SELECTED);
    }

    public string[] GetSelectedRow(params int[] columns)
    {
        var idx = GetSelectedIndex();
        if (idx < 0) return Array.Empty<string>();
        if (columns == null || columns.Length == 0) columns = new[] { 0 };
        var arr = new string[columns.Length];
        for (int i = 0; i < columns.Length; i++) arr[i] = GetText(idx, columns[i]);
        return arr;
    }
}
