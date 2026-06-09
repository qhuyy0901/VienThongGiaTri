// Premium High-Tech Light/Dark Theme Switcher Logic
document.addEventListener("DOMContentLoaded", function () {
    const body = document.body;
    const themeBtn = document.getElementById("themeToggleBtn");
    
    if (!themeBtn) return;
    
    const darkIcon = themeBtn.querySelector(".theme-icon-dark");
    const lightIcon = themeBtn.querySelector(".theme-icon-light");

    // 1. Load theme preference from localStorage or default to current HTML class
    const savedTheme = localStorage.getItem("theme");
    if (savedTheme) {
        body.classList.remove("theme-light", "theme-dark");
        body.classList.add(savedTheme);
    }

    // 2. Adjust toggle button icon states initially
    updateThemeIcons();

    // 3. Add click event listener to the switcher button
    themeBtn.addEventListener("click", function () {
        if (body.classList.contains("theme-light")) {
            body.classList.replace("theme-light", "theme-dark");
            localStorage.setItem("theme", "theme-dark");
        } else {
            body.classList.replace("theme-dark", "theme-light");
            localStorage.setItem("theme", "theme-light");
        }
        updateThemeIcons();
    });

    function updateThemeIcons() {
        if (body.classList.contains("theme-dark")) {
            darkIcon.style.display = "none";
            lightIcon.style.display = "inline-block";
        } else {
            darkIcon.style.display = "inline-block";
            lightIcon.style.display = "none";
        }
    }
});

// ══════════════════════════════════════
// GLOBAL TOAST NOTIFICATION ENGINE
// ══════════════════════════════════════
function showNotification(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container-custom';
        document.body.appendChild(container);
    }
    
    const toast = document.createElement('div');
    toast.className = `toast-item toast-${type}`;
    
    let icon = 'fa-circle-check';
    if (type === 'error') icon = 'fa-circle-exclamation';
    if (type === 'info') icon = 'fa-circle-info';
    
    toast.innerHTML = `
        <i class="fa-solid ${icon} toast-icon"></i>
        <div class="toast-content">
            <span class="toast-message">${message}</span>
        </div>
        <button class="toast-close" onclick="this.parentElement.remove()">&times;</button>
    `;
    
    container.appendChild(toast);
    
    // Auto-remove after 4 seconds
    setTimeout(() => {
        toast.classList.add('toast-fade-out');
        setTimeout(() => {
            toast.remove();
        }, 300);
    }, 4000);
}

// ══════════════════════════════════════
// AJAX ADD TO CART GLOBAL ACTION
// ══════════════════════════════════════
function addToCartAjax(productId, quantity = 1) {
    $.ajax({
        url: '/api/cart',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ productId: productId, quantity: quantity }),
        success: function (res) {
            if (res.success) {
                // Update badge count
                const badge = document.getElementById('cartBadgeCount');
                if (badge) {
                    badge.innerHTML = res.totalCount;
                    // Trigger dynamic pop scale animation
                    badge.style.transform = 'scale(1.3)';
                    setTimeout(() => {
                        badge.style.transform = 'scale(1)';
                    }, 250);
                }
                
                // Show gorgeous notification
                showNotification(res.message, 'success');
            } else {
                showNotification(res.message, 'error');
            }
        },
        error: function (xhr) {
            let errorMsg = 'Không thể kết nối đến máy chủ. Vui lòng thử lại!';
            if (xhr.responseJSON && xhr.responseJSON.message) {
                errorMsg = xhr.responseJSON.message;
            }
            showNotification(errorMsg, 'error');
        }
    });
}
