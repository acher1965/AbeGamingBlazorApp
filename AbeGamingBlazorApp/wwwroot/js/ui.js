window.abegamingUi = window.abegamingUi || {};

window.abegamingUi.scrollIntoViewById = function (id) {
  const el = document.getElementById(id);
  if (el) {
    el.scrollIntoView({ behavior: "smooth" });
  }
};
