(() => {
    const root = document.documentElement;
    const shell = document.querySelector(".khung-quan-tri");
    const openButton = document.querySelector("[data-open-menu]");
    const closeButton = document.querySelector("[data-close-menu]");
    const themeButton = document.querySelector("[data-theme-toggle]");
    const themeLabel = document.querySelector("[data-theme-label]");
    const themeIcon = document.querySelector("[data-theme-icon]");

    const updateThemeButton = () => {
        const isDark = root.dataset.theme === "dark";
        themeButton.setAttribute("aria-label", isDark ? "Chuyển giao diện sáng" : "Chuyển giao diện tối");
        themeLabel.textContent = isDark ? "Giao diện sáng" : "Giao diện tối";
        themeIcon.textContent = isDark ? "☼" : "◐";
    };

    themeButton.addEventListener("click", () => {
        root.dataset.theme = root.dataset.theme === "dark" ? "light" : "dark";
        localStorage.setItem("may-admin-theme", root.dataset.theme);
        updateThemeButton();
    });

    const setMenuOpen = (isOpen) => {
        shell.classList.toggle("menu-dang-mo", isOpen);
        openButton.setAttribute("aria-expanded", String(isOpen));
        if (isOpen) {
            closeButton.focus();
        } else {
            openButton.focus();
        }
    };

    openButton.addEventListener("click", () => setMenuOpen(true));
    closeButton.addEventListener("click", () => setMenuOpen(false));
    document.querySelectorAll(".dieu-huong-quan-tri a").forEach((link) => {
        link.addEventListener("click", () => {
            if (window.matchMedia("(max-width: 760px)").matches) {
                shell.classList.remove("menu-dang-mo");
                openButton.setAttribute("aria-expanded", "false");
            }
        });
    });
    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && shell.classList.contains("menu-dang-mo")) {
            setMenuOpen(false);
        }
    });

    document.querySelectorAll("form[data-confirm]").forEach((form) => {
        form.addEventListener("submit", (event) => {
            if (!window.confirm(form.dataset.confirm)) {
                event.preventDefault();
            }
        });
    });

    const previewUrls = new WeakMap();
    const formatFileSize = (size) => {
        if (size < 1024 * 1024) {
            return `${Math.max(1, Math.round(size / 1024))} KB`;
        }
        return `${(size / (1024 * 1024)).toFixed(1)} MB`;
    };

    document.querySelectorAll("input[type='file'][data-max-files]").forEach((input) => {
        input.addEventListener("change", () => {
            const maxFiles = Number(input.dataset.maxFiles);
            const container = input.closest("form, fieldset");
            const error = container?.querySelector("[data-file-error]");
            const preview = container?.querySelector("[data-file-preview]");
            const previousUrls = previewUrls.get(input) || [];
            previousUrls.forEach((url) => URL.revokeObjectURL(url));
            previewUrls.set(input, []);
            if (preview) {
                preview.replaceChildren();
                preview.classList.remove("co-tep-xem-truoc");
            }

            if (input.files.length > maxFiles) {
                if (error) {
                    error.textContent = `Mỗi lần chỉ chọn tối đa ${maxFiles} ảnh.`;
                }
                input.value = "";
                return;
            }

            if (error) {
                error.textContent = "";
            }

            if (!preview || input.files.length === 0) {
                return;
            }

            const urls = [];
            [...input.files].forEach((file) => {
                const item = document.createElement("div");
                item.className = "tep-xem-truoc";

                if (file.type.startsWith("image/") || /\.(jpe?g|png|webp|gif)$/i.test(file.name)) {
                    const url = URL.createObjectURL(file);
                    urls.push(url);
                    const image = document.createElement("img");
                    image.src = url;
                    image.alt = "";
                    image.addEventListener("error", () => {
                        image.remove();
                        const notice = document.createElement("span");
                        notice.className = "xem-truoc-khong-ho-tro";
                        notice.textContent = "Không thể xem trước ảnh này";
                        item.prepend(notice);
                    }, { once: true });
                    item.append(image);
                }

                const details = document.createElement("span");
                details.className = "tep-xem-truoc-thong-tin";
                const fileName = document.createElement("strong");
                fileName.textContent = file.name;
                const fileSize = document.createElement("small");
                fileSize.textContent = formatFileSize(file.size);
                details.append(fileName, fileSize);
                item.append(details);
                preview.append(item);
            });
            previewUrls.set(input, urls);
            preview.classList.add("co-tep-xem-truoc");
        });
    });

    updateThemeButton();
})();
