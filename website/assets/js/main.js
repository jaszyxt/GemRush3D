/* Gem Rush 3D website — progressive enhancement.
   Everything works without this file; this only adds polish. */
(function () {
  "use strict";

  var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  /* ----- Sticky header state ------------------------------------------- */
  var header = document.querySelector("[data-header]");
  if (header) {
    var onScroll = function () {
      header.classList.toggle("scrolled", window.scrollY > 24);
    };
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
  }

  /* ----- Mobile navigation ---------------------------------------------- */
  var toggle = document.querySelector("[data-nav-toggle]");
  var menu = document.querySelector("[data-nav-menu]");
  if (toggle && menu) {
    var closeMenu = function () {
      menu.classList.remove("open");
      toggle.setAttribute("aria-expanded", "false");
    };
    toggle.addEventListener("click", function () {
      var open = menu.classList.toggle("open");
      toggle.setAttribute("aria-expanded", String(open));
    });
    menu.addEventListener("click", function (e) {
      if (e.target.closest("a")) closeMenu();
    });
    document.addEventListener("keydown", function (e) {
      if (e.key === "Escape") closeMenu();
    });
    document.addEventListener("click", function (e) {
      if (!menu.contains(e.target) && !toggle.contains(e.target)) closeMenu();
    });
  }

  /* ----- Reveal on scroll ------------------------------------------------ */
  var revealables = Array.prototype.slice.call(document.querySelectorAll(".reveal"));
  if (revealables.length && "IntersectionObserver" in window && !reduceMotion) {
    var io = new IntersectionObserver(
      function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            var el = entry.target;
            var delay = revealables.filter(function (r) {
              return r.classList.contains("is-in");
            }).length % 6;
            el.style.transitionDelay = delay * 70 + "ms";
            el.classList.add("is-in");
            io.unobserve(el);
          }
        });
      },
      { threshold: 0.12, rootMargin: "0px 0px -40px 0px" }
    );
    revealables.forEach(function (el) { io.observe(el); });
  } else {
    revealables.forEach(function (el) { el.classList.add("is-in"); });
  }

  /* ----- Lightbox --------------------------------------------------------- */
  var lightbox = document.querySelector("[data-lightbox]");
  var shotButtons = Array.prototype.slice.call(
    document.querySelectorAll("[data-lightbox-open]")
  );
  if (lightbox && shotButtons.length && typeof lightbox.showModal === "function") {
    var lbImg = lightbox.querySelector("[data-lightbox-img]");
    var lbCaption = lightbox.querySelector("[data-lightbox-caption]");
    var btnClose = lightbox.querySelector("[data-lightbox-close]");
    var btnPrev = lightbox.querySelector("[data-lightbox-prev]");
    var btnNext = lightbox.querySelector("[data-lightbox-next]");
    var current = 0;
    var lastTrigger = null;

    var show = function (index) {
      current = (index + shotButtons.length) % shotButtons.length;
      var btn = shotButtons[current];
      lbImg.src = btn.getAttribute("data-full");
      lbImg.alt = btn.querySelector("img").alt;
      lbCaption.textContent = btn.getAttribute("data-caption");
    };

    shotButtons.forEach(function (btn, i) {
      btn.addEventListener("click", function () {
        lastTrigger = btn;
        show(i);
        lightbox.showModal();
      });
    });

    if (btnClose) {
      btnClose.addEventListener("click", function () { lightbox.close(); });
    }
    if (btnPrev) {
      btnPrev.addEventListener("click", function () { show(current - 1); });
    }
    if (btnNext) {
      btnNext.addEventListener("click", function () { show(current + 1); });
    }
    lightbox.addEventListener("keydown", function (e) {
      if (e.key === "ArrowLeft") show(current - 1);
      if (e.key === "ArrowRight") show(current + 1);
    });
    /* Click on the backdrop (outside the figure) closes. */
    lightbox.addEventListener("click", function (e) {
      var rect = lightbox.querySelector("figure").getBoundingClientRect();
      var inside =
        e.clientX >= rect.left && e.clientX <= rect.right &&
        e.clientY >= rect.top && e.clientY <= rect.bottom;
      if (!inside) lightbox.close();
    });
    lightbox.addEventListener("close", function () {
      if (lastTrigger) lastTrigger.focus();
    });
  }

  /* ----- Live release version --------------------------------------------- */
  var versionEls = document.querySelectorAll("[data-release-version]");
  if (versionEls.length) {
    fetch("https://api.github.com/repos/jaszyxt/GemRush3D/releases/latest", {
      headers: { Accept: "application/vnd.github+json" }
    })
      .then(function (res) { return res.ok ? res.json() : null; })
      .then(function (data) {
        if (!data || !data.tag_name) return;
        versionEls.forEach(function (el) { el.textContent = data.tag_name; });
        var note = document.querySelector("[data-release-note]");
        if (note && data.published_at) {
          var d = new Date(data.published_at);
          note.textContent = " · updated " + d.toLocaleDateString(undefined, {
            year: "numeric", month: "short", day: "numeric"
          });
        }
      })
      .catch(function () {
        /* Keep the hardcoded fallback version. */
      });
  }
})();
