using UnityEngine;
using UnityEngine.UI;

namespace GemRush
{
    /// Explicit UI navigation (directive D5): every direction on every
    /// menu is decided at build time, never left to uGUI's automatic
    /// finder. Grids wrap per row (left of the first cell is the row's
    /// last); chains run straight up/down.
    public static class MenuNav
    {
        public static void Set(Button button, Button up, Button down,
            Button left, Button right)
        {
            if (button == null) return;
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = up;
            nav.selectOnDown = down;
            nav.selectOnLeft = left;
            nav.selectOnRight = right;
            button.navigation = nav;
        }

        /// Vertical chain: each button links up/down with its neighbours.
        public static void Chain(params Button[] order)
        {
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == null) continue;
                Set(order[i],
                    up: i > 0 ? order[i - 1] : null,
                    down: i < order.Length - 1 ? order[i + 1] : null,
                    left: null, right: null);
            }
        }

        /// Row-major grid with per-row horizontal wrap and a fallback
        /// target above the top row. A short last row (fewer cells than
        /// cols) is handled: its up/down partners clamp to that row's
        /// actual width.
        public static void Grid(Button[] cells, int cols, Button upFallback)
        {
            if (cells == null || cells.Length == 0) return;
            int rows = (cells.Length + cols - 1) / cols;
            for (int i = 0; i < cells.Length; i++)
            {
                int row = i / cols;
                int col = i % cols;
                int rowLen = (row == rows - 1) ? cells.Length - row * cols : cols;
                // Only the last row can be short, so a row's predecessors
                // are always full width.
                Button up = row > 0 ? cells[(row - 1) * cols + col] : upFallback;
                Button down = null;
                if (row < rows - 1)
                {
                    int nextLen = (row + 1 == rows - 1)
                        ? cells.Length - (row + 1) * cols : cols;
                    down = cells[(row + 1) * cols + Mathf.Min(col, nextLen - 1)];
                }
                Button left = cells[row * cols + (col + rowLen - 1) % rowLen];
                Button right = cells[row * cols + (col + 1) % rowLen];
                Set(cells[i], up, down, left, right);
            }
        }
    }
}
