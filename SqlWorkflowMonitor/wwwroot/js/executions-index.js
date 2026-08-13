"use strict";

document.addEventListener("DOMContentLoaded", function () {
    document
        .querySelectorAll(".localized-date-picker")
        .forEach(function (picker) {
            const nativeInput =
                picker.querySelector(".localized-date-native");
            const displayInput =
                picker.querySelector(".localized-date-display");
            const culture = picker.dataset.culture;

            if (!nativeInput || !displayInput) {
                return;
            }

            function updateDisplayedDate() {
                if (!nativeInput.value) {
                    displayInput.value = "";
                    return;
                }

                const parts = nativeInput.value.split("-");

                if (parts.length !== 3) {
                    displayInput.value = "";
                    return;
                }

                const year = parts[0];
                const month = parts[1];
                const day = parts[2];

                displayInput.value = culture === "es-AR"
                    ? `${day}/${month}/${year}`
                    : `${month}/${day}/${year}`;
            }

            nativeInput.addEventListener(
                "change",
                updateDisplayedDate);

            updateDisplayedDate();
        });

    const pageSize = document.getElementById("pageSize");

    if (pageSize instanceof HTMLSelectElement) {
        pageSize.addEventListener("change", function () {
            pageSize.form?.requestSubmit();
        });
    }

    window.setTimeout(function () {
        window.location.reload();
    }, 30000);
});
