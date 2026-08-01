(function () {
  var me = document.currentScript;
  var widgetId = me.getAttribute("data-widget-id");
  var base = new URL(me.src).origin;

  var box = document.createElement("div");
  me.parentNode.insertBefore(box, me);

  fetch(base + "/widgets/" + widgetId + "/config")
    .then(function (r) {
      if (!r.ok) throw new Error("config " + r.status);
      return r.json();
    })
    .then(function (cfg) {
      render(cfg);
    })
    .catch(function (e) {
      console.error("[widget]", e);
    });

  function fieldsHtml(cfg) {
    return cfg.fields
      .map(function (f) {
        return (
          "<label>" +
          escape(f.label) +
          "<br>" +
          '<input name="' +
          escape(f.name) +
          '"></label><br>'
        );
      })
      .join("");
  }

  function formHtml(cfg) {
    return (
      "<h3>" +
      escape(cfg.title) +
      "</h3>" +
      fieldsHtml(cfg) +
      '<input name="website" style="display:none" tabindex="-1" autocomplete="off">' +
      '<button type="button" class="send">Pošalji</button>' +
      '<p class="msg"></p>'
    );
  }

  function render(cfg) {
    if (cfg.type === "cta") {
      box.innerHTML = '<a href="#">' + escape(cfg.title) + "</a>";
      return;
    }

    if (cfg.type === "popover") {
      box.innerHTML =
        '<div style="position:fixed;bottom:16px;right:16px;z-index:9999;max-width:300px;' +
        "background:#fff;border-radius:8px;padding:16px;font-family:sans-serif;" +
        'box-shadow:0 4px 16px rgba(0,0,0,.2)">' +
        '<button type="button" class="close" style="float:right;border:0;background:none;' +
        'font-size:18px;cursor:pointer">&times;</button>' +
        formHtml(cfg) +
        "</div>";

      box.querySelector(".close").addEventListener("click", function () {
        box.innerHTML = "";
      });
    } else {
      box.innerHTML =
        '<div style="border:1px solid #ccc;padding:16px;max-width:320px;font-family:sans-serif">' +
        formHtml(cfg) +
        "</div>";
    }

    box.querySelector(".send").addEventListener("click", function () {
      send(cfg);
    });
  }

  function send(cfg) {
    var data = {};
    cfg.fields.forEach(function (f) {
      data[f.name] = box.querySelector('[name="' + f.name + '"]').value;
    });

    fetch(base + "/submissions", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        widgetId: widgetId,
        data: data,
        website: box.querySelector('[name="website"]').value,
      }),
    })
      .then(function (r) {
        box.querySelector(".msg").textContent = r.ok
          ? "Hvala!"
          : "Greška (" + r.status + ")";
      })
      .catch(function () {
        box.querySelector(".msg").textContent = "Greška u mreži";
      });
  }

  function escape(s) {
    var d = document.createElement("div");
    d.textContent = s;
    return d.innerHTML;
  }
})();
