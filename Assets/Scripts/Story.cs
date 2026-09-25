using UnityEngine;

namespace GemRush
{
    /// All text-only story content that is not part of a level definition:
    /// the completion epilogue and the menu's rotating flavor quotes. Level
    /// missions, win lines and checkpoint beats live with their levels.
    public static class Story
    {
        /// Shown page by page on the completion screen, after the last
        /// level of the last pack is cleared.
        public static readonly string[] Epilogue = new string[]
        {
            "The Master Sunstone rose out of the Undercloud like a second dawn. " +
            "One by one, every portal in the sky realm lit at once — and the storm " +
            "that had swallowed them all simply... stopped.",

            "The Sky-Keeper examined Gloomfang for a long moment. A storm who stole " +
            "light because he feared the dark was still, technically, a weather " +
            "management problem. She gave him a badge and a rounder, kinder job: " +
            "carrying rain to the dry islands on Tuesdays.",

            "And Pip? Pip went back to the little island where all of this started, " +
            "put the last gem on the shelf, and watched two suns set. " +
            "Somewhere far below, a very large storm was learning to hum — and " +
            "somewhere above the garden, a very small one was perfecting the snore.",

            "The Far Isles chart hangs in the Sky-Keeper's hall — every island " +
            "named in Pip's small, determined handwriting. Then came the quiet " +
            "week, when the realm froze still and Pip carried the lantern " +
            "through it; and the festival, when the sky said thank you in " +
            "lights; and the long walk home, past the see-saws, back to the " +
            "little island where all of this started.\n\n" +
            "THE END — every ending here is just a portal to the next adventure."
        };

        /// One of these is shown in the menu's corner, rotating per visit.
        public static readonly string[] MenuQuotes = new string[]
        {
            "Gloomfang was never the villain. He was just weather with feelings.",
            "Gloomfang's diary, page 1: 'Today I stole the sun again. Still lonely.'",
            "The Sky-Keeper's review of Pip: 'small, determined, excellent at falling upward.'",
            "A storm's heart is a lantern nobody lit. Pip fixes that.",
            "Five lives. One very apologetic storm. And the realm keeps growing.",
            "The guardians spin because nobody ever asked them to stop. Ask nicely.",
            "The Two Suns playground: no storm, no pressure, all bounce.",
            "The Far Isles: where the wind does the climbing.",
            "Gloomfang's job title, officially: weather support. Tuesdays, mostly. He's very serious about Tuesdays.",
            "Nim's first word was a gust. There was weather everywhere. Gloomfang has never been prouder.",
            "The bell towers once sang storms home. Now they ring one in to work.",
            "Red Nine still dreams of being a carousel. On weekends, he practices.",
            "The Sky-Keeper's map has one word in the corner: 'more.' It's a to-do list.",
            "Every legend starts somewhere small. Pip's started with a stolen sun.",
            "The rematch: Gloomfang brought his best clouds. Pip brought snacks.",
            "Down is just a direction. Down here, someone was hoarding light for a very long time.",
            "A sky that copies you, half a second behind. Then half a second ahead. Nobody mentions it.",
            "After dark, the same places look different. So does the work.",
            "Snow holds still. Pip holds the lantern. Both are patient.",
            "The sky said thank you in colours it made itself.",
            "The road home was worth the atlas it took to find."
        };

        /// Index of the quote MenuQuote last returned, so voice lines can
        /// reference exactly the quote on screen.
        public static int LastQuoteIndex { get; private set; }

        public static string MenuQuote()
        {
            LastQuoteIndex = Random.Range(0, MenuQuotes.Length);
            return MenuQuotes[LastQuoteIndex];
        }
    }
}
