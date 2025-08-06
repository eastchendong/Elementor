using UnityEngine;
using System.Collections.Generic;

public class BookPagePrinter : MonoBehaviour
{
    public ScriptBoy.ProceduralBook.Book book;
    public int currentPageIndex; // Inspector里显示当前页码

    void Update()
    {
        if (book != null)
        {
            List<int> indices = new List<int>();
            book.GetActivePaperSideIndices(indices);
            if (indices.Count > 0)
            {
                currentPageIndex = indices[0]; // 或 indices[indices.Count-1]
            }
        }
    }

    public void PrintCurrentPage()
    {
        Debug.Log("当前显示页码索引: " + currentPageIndex);
    }
}