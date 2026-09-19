import io

p = 'Assets/Scripts/UIManager.cs'
s = io.open(p, encoding='utf-8').read()

old = """            pauseResumeButton = MakeButton(pausePanel.transform, Strings.Resume,
                new Vector2(0.5f, 0.40f), new Vector2(0f, 0f),
                new Vector2(360f, 84f), delegate { GameManager.Instance.ResumeGame(); });

            pauseRestartButton = MakeButton(pausePanel.transform, Strings.RestartLevel,
                new Vector2(0.5f, 0.285f), new Vector2(0f, 0f),
                new Vector2(360f, 68f),
                delegate { GameManager.Instance.PlayLevel(GameManager.Instance.CurrentLevel); });

            // Settings joins the pause menu (D9): sound/haptics/text size
            // are adjustable mid-run, without abandoning the level. MENU and
            // SETTINGS share the bottom row; the touch floor widens both,
            // which still clears side by side.
            pauseMenuButton = MakeButton(pausePanel.transform, Strings.Menu,
                new Vector2(0.5f - 0.13f, 0.165f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { GameManager.Instance.GoToMenu(); });

            pauseSettingsButton = MakeButton(pausePanel.transform, Strings.Settings,
                new Vector2(0.5f + 0.13f, 0.165f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { ShowSettings(); });

            MenuNav.Set(pauseResumeButton, null, pauseRestartButton, null, null);
            MenuNav.Set(pauseRestartButton, pauseResumeButton, pauseMenuButton,
                null, pauseSettingsButton);
            MenuNav.Set(pauseMenuButton, pauseRestartButton, null, null,
                pauseSettingsButton);
            MenuNav.Set(pauseSettingsButton, pauseRestartButton, null,
                pauseMenuButton, null);

            pausePanel.SetActive(false);
        }"""

new = """            pauseResumeButton = MakeButton(pausePanel.transform, Strings.Resume,
                new Vector2(0.5f, 0.42f), new Vector2(0f, 0f),
                new Vector2(360f, 84f), delegate { GameManager.Instance.ResumeGame(); });

            pauseRestartButton = MakeButton(pausePanel.transform, Strings.RestartLevel,
                new Vector2(0.5f, 0.30f), new Vector2(0f, 0f),
                new Vector2(360f, 68f),
                delegate { GameManager.Instance.PlayLevel(GameManager.Instance.CurrentLevel); });

            // Photo mode (photo postcards, DESIGN.md community plan): pause
            // the run, frame the sky, take the shot.
            pausePhotoButton = MakeButton(pausePanel.transform, Strings.Photo,
                new Vector2(0.5f, 0.185f), new Vector2(0f, 0f),
                new Vector2(360f, 60f), delegate { ShowPhotoMode(); });

            // Settings joins the pause menu (D9): sound/haptics/text size
            // are adjustable mid-run, without abandoning the level. MENU and
            // SETTINGS share the bottom row; the touch floor widens both,
            // which still clears side by side.
            pauseMenuButton = MakeButton(pausePanel.transform, Strings.Menu,
                new Vector2(0.5f - 0.13f, 0.075f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { GameManager.Instance.GoToMenu(); });

            pauseSettingsButton = MakeButton(pausePanel.transform, Strings.Settings,
                new Vector2(0.5f + 0.13f, 0.075f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { ShowSettings(); });

            MenuNav.Set(pauseResumeButton, null, pauseRestartButton, null, null);
            MenuNav.Set(pauseRestartButton, pauseResumeButton, pausePhotoButton,
                null, null);
            MenuNav.Set(pausePhotoButton, pauseRestartButton, pauseMenuButton,
                null, null);
            MenuNav.Set(pauseMenuButton, pausePhotoButton, null, null,
                pauseSettingsButton);
            MenuNav.Set(pauseSettingsButton, pausePhotoButton, null,
                pauseMenuButton, null);

            pausePanel.SetActive(false);
        }

        // ------------------------------------------------------------------
        // Photo mode: a slow orbit around Pip with a small capture bar.
        // Entered from pause (world frozen, camera drifts on the unscaled
        // clock); CAPTURE hides the bar for the frame the shot is taken,
        // saves a timestamped PNG into the player's Pictures folder, and
        // offers OPEN FOLDER. DONE returns to the pause menu.
        // ------------------------------------------------------------------
        GameObject photoPanel;
        Text photoStatus;
        Button photoOpenFolder;
        PhotoMode photoOrbit;
        string photoFolder;

        void BuildPhotoPanel(Transform canvas)
        {
            // A small bar at the bottom — the rest of the screen stays
            // open for framing the shot.
            photoPanel = new GameObject("PhotoPanel", typeof(RectTransform));
            photoPanel.transform.SetParent(canvas, false);
            RectTransform bar = photoPanel.GetComponent<RectTransform>();
            bar.anchorMin = new Vector2(0.5f, 0f);
            bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 8f);
            bar.sizeDelta = new Vector2(660f, 120f);

            Image backdrop = photoPanel.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.55f);

            photoStatus = MakeText(photoPanel.transform, "PhotoStatus",
                Strings.PhotoHint, 22, new Color(0.9f, 0.9f, 0.95f),
                TextAnchor.UpperCenter, new Vector2(0.05f, 0.62f),
                new Vector2(0.95f, 0.96f), 0f, 0f, 0f, 0f);

            MakeButton(photoPanel.transform, Strings.PhotoCapture,
                new Vector2(0.25f, 0.38f), new Vector2(0f, 0f),
                new Vector2(240f, 60f), delegate { CapturePhoto(); });
            photoOpenFolder = MakeButton(photoPanel.transform,
                Strings.PhotoOpenFolder, new Vector2(0.55f, 0.38f),
                new Vector2(0f, 0f), new Vector2(240f, 60f),
                delegate { OpenPhotoFolder(); });
            photoOpenFolder.gameObject.SetActive(false);
            MakeButton(photoPanel.transform, Strings.PhotoDone,
                new Vector2(0.85f, 0.38f), new Vector2(0f, 0f),
                new Vector2(180f, 60f), delegate { ClosePhotoMode(); });

            photoPanel.SetActive(false);
        }

        public void ShowPhotoMode()
        {
            if (GameBootstrap.CameraRig == null ||
                GameBootstrap.Player == null) return;
            HidePaused(); // the pause panel gets out of the shot
            photoFolder = System.IO.Path.Combine(
                System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.MyPictures),
                "GemRush3D");
            System.IO.Directory.CreateDirectory(photoFolder);
            photoStatus.text = Strings.PhotoHint;
            photoOpenFolder.gameObject.SetActive(false);
            photoPanel.SetActive(true);
            photoOrbit = PhotoMode.Begin(GameBootstrap.CameraRig,
                GameBootstrap.Player.transform);
            Button capture = photoPanel.GetComponentInChildren<Button>();
            Focus(capture);
            AudioManager.Instance.PlayPanel(true);
        }

        public void ClosePhotoMode()
        {
            if (!PhotoModeOpen) return;
            if (photoOrbit != null)
            {
                photoOrbit.End();
                photoOrbit = null;
            }
            photoPanel.SetActive(false);
            ShowPaused(); // back to the pause menu, run still paused
            AudioManager.Instance.PlayPanel(false);
        }

        public bool PhotoModeOpen
        {
            get { return photoPanel != null && photoPanel.activeSelf; }
        }

        /// CAPTURE: hide the bar for the frame the shot is taken (the
        /// screenshot is composited at end of frame), save a timestamped
        /// PNG at 2x into the Pictures folder, then restore the bar.
        void CapturePhoto()
        {
            string path = System.IO.Path.Combine(photoFolder,
                "gemrush_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            StartCoroutine(CapturePhotoRoutine(path));
        }

        System.Collections.IEnumerator CapturePhotoRoutine(string path)
        {
            photoPanel.SetActive(false);
            yield return null; // the hidden-bar frame; capture composites now
            ScreenCapture.CaptureScreenshot(path, 2);
            yield return null; // give the capture a frame to land
            photoPanel.SetActive(true);
            photoStatus.text = Strings.PhotoSavedTo(path);
            photoOpenFolder.gameObject.SetActive(true);
            AudioManager.Instance.PlayStarDing(2);
        }

        void OpenPhotoFolder()
        {
            Application.OpenURL(photoFolder);
        }"""

assert old in s, 'pause block not found'
s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('pause + photo mode written')
