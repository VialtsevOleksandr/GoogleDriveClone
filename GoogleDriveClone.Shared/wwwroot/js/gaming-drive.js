// Essential JavaScript functions for Gaming Drive
// Only browser-specific functionality that can't be done in C#

// File download function (requires browser API)
window.downloadFile = (url, fileName) => {
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Enhanced file download with proper authorization headers
window.downloadFileFromUrl = async (url, fileName) => {
    try {
        // Get auth token from localStorage if available
        const authToken = localStorage.getItem('authToken');
        
        const headers = {};
        if (authToken) {
            headers['Authorization'] = `Bearer ${authToken}`;
        }
        
        // Fetch the file with proper headers
        const response = await fetch(url, {
            method: 'GET',
            headers: headers
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        // Create blob and download
        const blob = await response.blob();
        const downloadUrl = window.URL.createObjectURL(blob);
        
        const link = document.createElement('a');
        link.href = downloadUrl;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        
        // Cleanup
        document.body.removeChild(link);
        window.URL.revokeObjectURL(downloadUrl);
        
    } catch (error) {
        console.error('Download failed:', error);
        throw error; // Re-throw to be handled by C#
    }
};

// Check if download is supported
window.isDownloadSupported = () => {
    const a = document.createElement('a');
    return typeof a.download !== 'undefined';
};

// Notification system with SVG icons instead of emojis
window.showNotification = (message, type = 'info') => {
    const notification = document.createElement('div');
    notification.className = `gaming-notification ${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <div class="notification-icon">${getNotificationIcon(type)}</div>
            <span class="notification-text">${message}</span>
        </div>
    `;
    
    // Style the notification
    Object.assign(notification.style, {
        position: 'fixed',
        top: '20px',
        right: '20px',
        padding: '12px 20px',
        backgroundColor: getNotificationColor(type),
        color: 'white',
        borderRadius: '8px',
        fontFamily: 'Orbitron, monospace',
        fontWeight: 'bold',
        fontSize: '14px',
        zIndex: '9999',
        boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
        transform: 'translateX(100%)',
        transition: 'transform 0.3s ease',
        clipPath: 'polygon(8px 0%, 100% 0%, 100% calc(100% - 8px), calc(100% - 8px) 100%, 0% 100%, 0% 8px)',
        minWidth: '250px',
        maxWidth: '400px'
    });
    
    // Add to page
    document.body.appendChild(notification);
    
    // Animate in
    requestAnimationFrame(() => {
        notification.style.transform = 'translateX(0)';
    });
    
    // Remove after delay
    setTimeout(() => {
        notification.style.transform = 'translateX(100%)';
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    }, 4000);
};

function getNotificationColor(type) {
    switch (type) {
        case 'success': return '#10B981';
        case 'error': return '#EF4444';
        case 'warning': return '#F59E0B';
        default: return '#8B5CF6';
    }
}

// SVG icons instead of emojis for better consistency
function getNotificationIcon(type) {
    const iconSize = '16';
    switch (type) {
        case 'success': 
            return `<svg width="${iconSize}" height="${iconSize}" viewBox="0 0 24 24" fill="currentColor">
                <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41L9 16.17z"/>
            </svg>`;
        case 'error': 
            return `<svg width="${iconSize}" height="${iconSize}" viewBox="0 0 24 24" fill="currentColor">
                <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12 19 6.41z"/>
            </svg>`;
        case 'warning': 
            return `<svg width="${iconSize}" height="${iconSize}" viewBox="0 0 24 24" fill="currentColor">
                <path d="M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"/>
            </svg>`;
        default: 
            return `<svg width="${iconSize}" height="${iconSize}" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z"/>
            </svg>`;
    }
}

// Initialize drag and drop for file upload with better error handling
window.initializeDragAndDrop = (dropZoneId, dotNetReference) => {
    const dropZone = document.getElementById(dropZoneId);
    if (!dropZone) {
        console.warn(`Drop zone element with id '${dropZoneId}' not found`);
        return;
    }
    
    const eventNames = ['dragenter', 'dragover', 'dragleave', 'drop'];
    
    eventNames.forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
    });
    
    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }
    
    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => {
            dropZone.classList.add('drag-over');
        }, false);
    });
    
    ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => {
            dropZone.classList.remove('drag-over');
        }, false);
    });
    
    dropZone.addEventListener('drop', (e) => {
        const dt = e.dataTransfer;
        const files = Array.from(dt.files);
        
        if (files.length > 0) {
            try {
                dotNetReference.invokeMethodAsync('HandleDroppedFiles', files.map(f => ({
                    name: f.name,
                    size: f.size,
                    type: f.type
                })));
            } catch (error) {
                console.error('Error handling dropped files:', error);
            }
        }
    }, false);
};

// Keyboard shortcuts with better error handling
window.setupKeyboardShortcuts = (dotNetReference) => {
    if (!dotNetReference) {
        console.warn('DotNetReference is required for keyboard shortcuts');
        return;
    }

    document.addEventListener('keydown', (e) => {
        try {
            // Ctrl/Cmd + U for upload
            if ((e.ctrlKey || e.metaKey) && e.key === 'u') {
                e.preventDefault();
                dotNetReference.invokeMethodAsync('TriggerUpload');
            }
            
            // Ctrl/Cmd + F for search focus
            if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
                e.preventDefault();
                const searchInput = document.querySelector('.search-field') || document.querySelector('.search-box input');
                if (searchInput) {
                    searchInput.focus();
                    searchInput.select();
                }
            }
            
            // Escape to close modals
            if (e.key === 'Escape') {
                dotNetReference.invokeMethodAsync('CloseModals');
            }
        } catch (error) {
            console.error('Error in keyboard shortcut handler:', error);
        }
    });
};

// Focus management with better error handling
window.focusElement = (selector) => {
    if (!selector) return false;
    
    try {
        const element = document.querySelector(selector);
        if (element) {
            element.focus();
            if (element.select && typeof element.select === 'function') {
                element.select();
            }
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error focusing element:', error);
        return false;
    }
};

// Scroll to element with smooth animation
window.scrollToElement = (selector, behavior = 'smooth') => {
    if (!selector) return false;
    
    try {
        const element = document.querySelector(selector);
        if (element) {
            element.scrollIntoView({ 
                behavior: behavior, 
                block: 'center',
                inline: 'nearest'
            });
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error scrolling to element:', error);
        return false;
    }
};

// Get viewport size
window.getViewportSize = () => {
    return {
        width: window.innerWidth || document.documentElement.clientWidth,
        height: window.innerHeight || document.documentElement.clientHeight
    };
};

// Add CSS class with animation and cleanup
window.addClassWithAnimation = (selector, className, duration = 300) => {
    if (!selector || !className) return false;
    
    try {
        const element = document.querySelector(selector);
        if (element) {
            element.classList.add(className);
            setTimeout(() => {
                element.classList.remove(className);
            }, Math.max(duration, 0));
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error adding class with animation:', error);
        return false;
    }
};

// Utility function to check if element is visible
window.isElementVisible = (selector) => {
    try {
        const element = document.querySelector(selector);
        if (!element) return false;
        
        const rect = element.getBoundingClientRect();
        return rect.top >= 0 && rect.left >= 0 && 
               rect.bottom <= window.innerHeight && 
               rect.right <= window.innerWidth;
    } catch (error) {
        console.error('Error checking element visibility:', error);
        return false;
    }
};

// Copy text to clipboard
window.copyToClipboard = async (text) => {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        } else {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'absolute';
            textArea.style.left = '-9999px';
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
            return true;
        }
    } catch (error) {
        console.error('Error copying to clipboard:', error);
        return false;
    }
};