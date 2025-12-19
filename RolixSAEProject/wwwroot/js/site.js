// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", () => {
    const currencySwitch = document.querySelector(".currency-switch");
    const currencyButton = currencySwitch?.querySelector(".currency-button");
    const currencyMenu = currencySwitch?.querySelector(".currency-menu");

    if (!currencySwitch || !currencyButton || !currencyMenu) {
        return;
    }

    const closeMenu = () => {
        currencySwitch.classList.remove("open");
        currencyButton.setAttribute("aria-expanded", "false");
    };
    const openMenu = () => {
        currencySwitch.classList.add("open");
        currencyButton.setAttribute("aria-expanded", "true");
    };
    const toggleMenu = () => {
        if (currencySwitch.classList.contains("open")) {
            closeMenu();
        } else {
            openMenu();
        }
    };

    currencyButton.addEventListener("click", (event) => {
        event.preventDefault();
        toggleMenu();
    });

    currencyMenu.querySelectorAll("button").forEach((option) => {
        option.addEventListener("click", () => {
            closeMenu();
        });
    });

    document.addEventListener("click", (event) => {
        if (!currencySwitch.contains(event.target)) {
            closeMenu();
        }
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            closeMenu();
            currencyButton.focus();
        }
    });
});
