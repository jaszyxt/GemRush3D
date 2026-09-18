using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GemRush.Tests
{
    /// <summary>
    /// MenuNav wiring checks (directives D5): the explicit navigation the
    /// menus are wired with must wrap per row, bridge short last rows
    /// correctly and chain vertically. Pure GameObject math — no
    /// EventSystem or input backend required.
    /// </summary>
    [TestFixture]
    public class MenuNavTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        Button[] BuildCells(int count)
        {
            root = new GameObject("NavProbe");
            Button[] cells = new Button[count];
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("Cell" + i, typeof(RectTransform));
                go.transform.SetParent(root.transform, false);
                cells[i] = go.AddComponent<Button>();
            }
            return cells;
        }

        [Test]
        public void Grid_WrapsPerRow_AndBridgesRows()
        {
            Button[] cells = BuildCells(6); // 2 rows x 3
            MenuNav.Grid(cells, 3, null);

            // Each row wraps within itself.
            Assert.AreEqual(cells[2], cells[0].FindSelectableOnLeft(),
                "left of a row's first cell wraps to its last");
            Assert.AreEqual(cells[0], cells[2].FindSelectableOnRight(),
                "right of a row's last cell wraps to its first");
            Assert.AreEqual(cells[5], cells[3].FindSelectableOnLeft(),
                "second row wraps too");
            // Rows bridge on the same column.
            Assert.AreEqual(cells[1], cells[4].FindSelectableOnUp());
            Assert.AreEqual(cells[4], cells[1].FindSelectableOnDown());
        }

        [Test]
        public void Grid_ShortLastRow_ClampsAndSelfWraps()
        {
            Button[] cells = BuildCells(4); // row 0: 3 cells, row 1: 1 cell
            MenuNav.Grid(cells, 3, null);

            // The lone last-row cell wraps on itself, climbs to the top
            // row, and the top row's overflow column drops into it.
            Assert.AreEqual(cells[3], cells[3].FindSelectableOnLeft(),
                "a single-cell row wraps to itself");
            Assert.AreEqual(cells[0], cells[3].FindSelectableOnUp());
            Assert.AreEqual(cells[3], cells[2].FindSelectableOnDown(),
                "top row's last cell drops into the short row");
        }

        [Test]
        public void Grid_TopRow_UpFallsBackToTheGivenTarget()
        {
            Button[] cells = BuildCells(2);
            GameObject extra = new GameObject("Anchor", typeof(RectTransform));
            extra.transform.SetParent(root.transform, false);
            Button play = extra.AddComponent<Button>();

            MenuNav.Grid(cells, 2, play);
            Assert.AreEqual(play, cells[0].FindSelectableOnUp());
            Assert.AreEqual(play, cells[1].FindSelectableOnUp());
        }

        [Test]
        public void Chain_LinksUpAndDownWithDeadEnds()
        {
            Button[] cells = BuildCells(3);
            MenuNav.Chain(cells);

            Assert.IsNull(cells[0].FindSelectableOnUp(), "chain starts at the top");
            Assert.AreEqual(cells[1], cells[0].FindSelectableOnDown());
            Assert.AreEqual(cells[0], cells[1].FindSelectableOnUp());
            Assert.IsNull(cells[2].FindSelectableOnDown(), "chain ends at the bottom");
        }
    }
}
