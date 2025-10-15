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

// File validation function
window.validateFile = (file) => {
    try {
        if (!file) {
            return {
                isValid: false,
                error: 'Файл не знайдено або пошкоджений'
            };
        }
        
        if (!file.name || file.name.trim() === '') {
            return {
                isValid: false,
                error: 'Файл має порожню назву'
            };
        }
        
        // File constraints matching C# FileUploadService
        const maxFileSize = 50 * 1024 * 1024; // 50MB
        const allowedExtensions = [
            '.jpg', '.jpeg', '.png', '.gif', '.bmp', 
            '.txt', '.md', '.pdf', '.doc', '.docx',
            '.py', '.c', '.cpp', '.cs', '.js', '.html', '.css',
            '.zip', '.rar', '.7z'
        ];
        
        // Check file size
        if (file.size > maxFileSize) {
            const fileSizeMB = (file.size / (1024 * 1024)).toFixed(2);
            return {
                isValid: false,
                error: `Файл ${file.name} занадто великий (${fileSizeMB} МБ). Максимальний розмір: 50 МБ`
            };
        }
        
        // Skip validation for files without extension
        const lastDotIndex = file.name.lastIndexOf('.');
        if (lastDotIndex === -1) {
            return {
                isValid: false,
                error: `Файл ${file.name} не має розширення`
            };
        }
        
        // Check file extension
        const extension = file.name.toLowerCase().substring(lastDotIndex);
        if (!allowedExtensions.includes(extension)) {
            return {
                isValid: false,
                error: `Тип файлу ${extension} не підтримується. Підтримувані типи: ${allowedExtensions.join(', ')}`
            };
        }
        
        return { isValid: true };
        
    } catch (error) {
        return {
            isValid: false,
            error: `Помилка валідації файлу: ${error.message}`
        };
    }
};

// Get auth token from storage
window.getAuthToken = () => {
    let token = localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    
    // Remove quotes if they exist (fix for token stored with quotes)
    if (token && (token.startsWith('"') && token.endsWith('"'))) {
        token = token.slice(1, -1);
    }
    
    return token;
};

// ============================================================================
// FOLDER SYNCHRONIZATION FUNCTIONS
// ============================================================================

// Global variable to store directory handle (can't be serialized through Blazor)
let selectedDirectoryHandle = null;

// Check if File System Access API is supported
window.isFolderSyncSupported = () => {
    return 'showDirectoryPicker' in window;
};

// Select a folder using File System Access API
window.selectFolder = async () => {
    try {
        if (!window.isFolderSyncSupported()) {
            throw new Error('Ваш браузер не підтримує вибір папок. Використовуйте Chrome 86+ або Edge 86+');
        }

        const directoryHandle = await window.showDirectoryPicker({
            mode: 'read'
        });

        selectedDirectoryHandle = directoryHandle;

        return {
            success: true,
            name: directoryHandle.name,
            handleId: 'stored-globally'
        };
    } catch (error) {
        if (error.name === 'AbortError') {
            return {
                success: false,
                error: 'Вибір папки скасовано користувачем'
            };
        }
        
        return {
            success: false,
            error: `Помилка вибору папки: ${error.message}`
        };
    }
};

// Clear stored directory handle
window.clearStoredDirectoryHandle = () => {
    selectedDirectoryHandle = null;
};

// Check if directory handle is stored and still valid
window.hasValidDirectoryHandle = async () => {
    if (!selectedDirectoryHandle) {
        return false;
    }
    
    try {
        // Try to access the handle to check if it's still valid
        await selectedDirectoryHandle.getPermissionState({ mode: 'read' });
        return true;
    } catch (error) {
        selectedDirectoryHandle = null;
        return false;
    }
};

// Read all files from a directory handle (only root level)
// Now uses the globally stored handle instead of receiving it as parameter
window.readFolderFiles = async (handleRef, progressCallback) => {
    try {
        const directoryHandle = selectedDirectoryHandle;
        if (!directoryHandle) {
            throw new Error('Не знайдено збереженого дескриптора папки. Оберіть папку ще раз.');
        }
        
        const files = [];
        const entries = [];
        
        // Collect all entries first
        try {
            // Verify that directoryHandle has the entries method
            if (typeof directoryHandle.entries !== 'function') {
                throw new Error('Дескриптор папки не підтримує метод entries. Можливо, доступ було втратити.');
            }
            
            for await (const [name, handle] of directoryHandle.entries()) {
                // Skip subdirectories as per requirements
                if (handle.kind === 'file') {
                    entries.push({ name, handle });
                }
            }
        } catch (enumError) {
            if (enumError.name === 'NotAllowedError') {
                return {
                    success: false,
                    error: 'Доступ до папки заборонено. Можливо, дозволи застаріли. Оберіть папку ще раз.',
                    files: []
                };
            }
            
            return {
                success: false,
                error: `Помилка перегляду файлів у папці: ${enumError.message}`,
                files: []
            };
        }

        // Process files with progress
        for (let i = 0; i < entries.length; i++) {
            const { name, handle } = entries[i];
            
            try {
                // Update progress
                if (progressCallback) {
                    try {
                        await progressCallback.invokeMethodAsync('UpdateProgress', {
                            current: i + 1,
                            total: entries.length,
                            fileName: name,
                            message: `Обробка файлу ${name} (${i + 1}/${entries.length})...`
                        });
                    } catch (progressError) {
                        // Continue processing even if progress update fails
                    }
                }

                const file = await handle.getFile();
                
                // Validate file
                const validation = window.validateFile(file);
                if (!validation.isValid) {
                    continue;
                }

                // Calculate hash
                let hash;
                try {
                    hash = await window.calculateFileHash(file);
                } catch (hashError) {
                    continue;
                }
                
                const fileInfo = {
                    name: file.name,
                    size: file.size,
                    hash: hash,
                    file: file
                };
                
                files.push(fileInfo);

            } catch (fileError) {
                if (fileError.name === 'NotAllowedError') {
                    continue;
                }
                continue;
            }
        }

        window.storeProcessedFilesWithObjects(files);

        const result = {
            success: true,
            files: files,
            totalFiles: entries.length,
            validFiles: files.length
        };
        
        return result;

    } catch (error) {
        selectedDirectoryHandle = null;
        
        let errorMessage = `Критична помилка читання папки: ${error.message}`;
        
        if (error.name === 'NotAllowedError') {
            errorMessage = 'Доступ до папки заборонено. Спробуйте обрати іншу папку.';
        } else if (error.name === 'SecurityError') {
            errorMessage = 'Помилка безпеки при доступі до папки.';
        } else if (error.name === 'InvalidStateError') {
            errorMessage = 'Неправильний стан папки. Спробуйте обрати папку ще раз.';
        } else if (error.message.includes('дескриптора папки')) {
            errorMessage = error.message;
        }
        
        return {
            success: false,
            error: errorMessage,
            files: []
        };
    }
};

window.calculateFileHash = async (file) => {
    try {
        if (!file || file.size === 0) {
            return 'empty-file-hash';
        }
        
        const maxHashSize = 100 * 1024 * 1024; // 100MB limit for hash calculation
        let fileToHash = file;
        
        if (file.size > maxHashSize) {
            fileToHash = file.slice(0, maxHashSize);
        }
        
        const arrayBuffer = await fileToHash.arrayBuffer();
        const hashBuffer = await crypto.subtle.digest('SHA-256', arrayBuffer);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
        
        return hashHex;
        
    } catch (error) {
        const fallbackData = `${file?.name || 'unknown'}-${file?.size || 0}-${file?.lastModified || Date.now()}`;
        const encoder = new TextEncoder();
        const data = encoder.encode(fallbackData);
        
        try {
            const hashBuffer = await crypto.subtle.digest('SHA-256', data);
            const hashArray = Array.from(new Uint8Array(hashBuffer));
            const fallbackHash = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
            return fallbackHash;
        } catch (fallbackError) {
            throw new Error(`Не вдалося обчислити хеш файлу ${file?.name || 'unknown'}: ${error.message}`);
        }
    }
};

// Compare local files with server files and determine sync actions
window.compareFolderFiles = (localFiles, serverFiles) => {
    try {
        const actions = [];
        const serverFileMap = new Map();
        
        const localFilesArray = typeof localFiles === 'string' ? JSON.parse(localFiles) : localFiles;
        const serverFilesArray = typeof serverFiles === 'string' ? JSON.parse(serverFiles) : serverFiles;
        
        // Create a map of server files by name
        serverFilesArray.forEach(serverFile => {
            const normalizedName = serverFile.OriginalName.normalize('NFC');
            
            if (!serverFileMap.has(normalizedName)) {
                serverFileMap.set(normalizedName, []);
            }
            serverFileMap.get(normalizedName).push(serverFile);
        });

        // Process each local file
        localFilesArray.forEach(localFile => {
            const fileName = (localFile.name || localFile.Name).normalize('NFC');
            const fileHash = localFile.hash || localFile.Hash;
            
            const serverFiles = serverFileMap.get(fileName) || [];
            
            const exactMatchFile = serverFiles.find(sf => sf.FileHash === fileHash);
            
            if (exactMatchFile) {
                return;
            }
            
            const fileToReplace = serverFiles.find(sf => sf.FileHash !== fileHash);
            
            if (!fileToReplace) {
                actions.push({
                    type: 'upload',
                    fileName: fileName,
                    localFile: {
                        name: fileName,
                        size: localFile.size || localFile.Size,
                        hash: fileHash
                    },
                    reason: 'Новий файл'
                });
            } else {
                actions.push({
                    type: 'replace',
                    fileName: fileName,
                    localFile: {
                        name: fileName,
                        size: localFile.size || localFile.Size,
                        hash: fileHash
                    },
                    serverFile: {
                        id: fileToReplace.Id,
                        originalName: fileToReplace.OriginalName,
                        fileHash: fileToReplace.FileHash
                    },
                    serverFileId: fileToReplace.Id,
                    reason: 'Файл змінено'
                });
            }
        });
        
        const summary = {
            totalLocalFiles: localFilesArray.length,
            newFiles: actions.filter(a => a.type === 'upload').length,
            replacedFiles: actions.filter(a => a.type === 'replace').length,
            unchangedFiles: localFilesArray.length - actions.length
        };

        window.storeProcessedFiles(localFilesArray);

        return {
            success: true,
            actions: actions,
            summary: summary
        };

    } catch (error) {
        return {
            success: false,
            error: `Помилка порівняння файлів: ${error.message}`,
            actions: [],
            summary: {}
        };
    }
};

// Store processed files globally for sync operations
let processedFiles = new Map();

// Store files WITH File objects (called from readFolderFiles before serialization)
window.storeProcessedFilesWithObjects = (files) => {
    processedFiles.clear();
    files.forEach(fileInfo => {
        const fileName = fileInfo.name;
        if (fileName && fileInfo.file) {
            processedFiles.set(fileName, fileInfo);
        }
    });
};

// Function to store processed files after readFolderFiles (metadata only)
window.storeProcessedFiles = (files) => {
    // This is called from compareFolderFiles but only updates metadata
    // Don't clear the map here
};

// Clear all sync-related stored data
window.clearSyncData = () => {
    selectedDirectoryHandle = null;
    processedFiles.clear();
};

// Perform folder synchronization
window.performFolderSync = async (syncActions, progressCallback) => {
    try {
        const actions = JSON.parse(syncActions);
        const results = [];
        let successCount = 0;
        let errorCount = 0;

        for (let i = 0; i < actions.length; i++) {
            const action = actions[i];
            
            try {
                // Update progress
                if (progressCallback) {
                    try {
                        await progressCallback.invokeMethodAsync('UpdateProgress', {
                            current: i + 1,
                            total: actions.length,
                            action: action,
                            message: `${action.type === 'upload' ? 'Завантажую' : 'Замінюю'} ${action.fileName} (${i + 1}/${actions.length})...`
                        });
                    } catch (progressError) {
                        // Ignore progress errors
                    }
                }

                if (action.type === 'upload') {
                    let file = null;
                    
                    if (action.localFile && action.localFile.file) {
                        file = action.localFile.file;
                    } else {
                        const storedFile = processedFiles.get(action.fileName);
                        if (storedFile && storedFile.file) {
                            file = storedFile.file;
                        }
                    }
                    
                    if (!file) {
                        throw new Error(`Не знайдено файл ${action.fileName} для завантаження`);
                    }
                    
                    const uploadResult = await window.uploadSingleFile(file);
                    
                    if (uploadResult.success) {
                        successCount++;
                        results.push({
                            fileName: action.fileName,
                            action: 'uploaded',
                            success: true
                        });
                    } else {
                        errorCount++;
                        results.push({
                            fileName: action.fileName,
                            action: 'upload_failed',
                            success: false,
                            error: uploadResult.error
                        });
                    }
                    
                } else if (action.type === 'replace') {
                    let serverFileId = action.serverFileId || 
                                      action.ServerFileId || 
                                      action.serverFile?.id || 
                                      action.serverFile?.Id || 
                                      action.serverFile?.ID;
                    
                    if (!serverFileId) {
                        throw new Error(`Не знайдено ID серверного файлу для заміни ${action.fileName}`);
                    }
                    
                    const deleteResult = await window.deleteSingleFile(serverFileId);
                    
                    if (deleteResult.success) {
                        let file = null;
                        
                        if (action.localFile && action.localFile.file) {
                            file = action.localFile.file;
                        } else {
                            const storedFile = processedFiles.get(action.fileName);
                            if (storedFile && storedFile.file) {
                                file = storedFile.file;
                            }
                        }
                        
                        if (!file) {
                            throw new Error(`Не знайдено файл ${action.fileName} для заміни`);
                        }
                        
                        const uploadResult = await window.uploadSingleFile(file);
                        
                        if (uploadResult.success) {
                            successCount++;
                            results.push({
                                fileName: action.fileName,
                                action: 'replaced',
                                success: true
                            });
                        } else {
                            errorCount++;
                            results.push({
                                fileName: action.fileName,
                                action: 'replace_upload_failed',
                                success: false,
                                error: uploadResult.error
                            });
                        }
                    } else {
                        errorCount++;
                        results.push({
                            fileName: action.fileName,
                            action: 'replace_delete_failed',
                            success: false,
                            error: deleteResult.error
                        });
                    }
                }
            } catch (actionError) {
                errorCount++;
                results.push({
                    fileName: action.fileName,
                    action: 'error',
                    success: false,
                    error: actionError.message
                });
            }
        }

        return {
            success: true,
            results: results,
            summary: {
                total: actions.length,
                success: successCount,
                errors: errorCount
            }
        };

    } catch (error) {
        return {
            success: false,
            error: `Помилка синхронізації: ${error.message}`,
            results: []
        };
    }
};

// Helper function to detect if we're running in MAUI
window.isMauiEnvironment = () => {
    // Check for MAUI-specific indicators
    return typeof window.__MAUI__ !== 'undefined' || 
           window.location.protocol === 'app:' ||
           window.location.hostname === '0.0.0.0' ||
           navigator.userAgent.includes('.NET MAUI');
};

// Helper function to get the correct API base URL
window.getApiBaseUrl = () => {
    // First check if base URL was configured from C#
    if (typeof window.__GAMING_DRIVE_API_BASE__ !== 'undefined' && window.__GAMING_DRIVE_API_BASE__) {
        return window.__GAMING_DRIVE_API_BASE__;
    }
    
    // Fallback: detect environment
    if (window.isMauiEnvironment()) {
        // For MAUI, use configured base address (should be set by C#)
        return 'https://localhost:7166';
    } else {
        // For Blazor Web, check if we need to use different port
        if (window.location.port === '7169') {
            const currentProtocol = window.location.protocol;
            const currentHost = window.location.hostname;
            return `${currentProtocol}//${currentHost}:7166`;
        }
        return '';
    }
};

// Upload a single file to the server
window.uploadSingleFile = async (file) => {
    try {
        const authToken = window.getAuthToken();
        if (!authToken) {
            throw new Error('Помилка авторизації');
        }

        const formData = new FormData();
        formData.append('File', file);

        const baseUrl = window.getApiBaseUrl();
        const apiUrl = `${baseUrl}/api/files/upload`;

        const response = await fetch(apiUrl, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${authToken}`
            },
            body: formData
        });

        if (response.ok) {
            const result = await response.json();
            return {
                success: result.success,
                error: result.success ? null : result.message
            };
        } else {
            const errorText = await response.text();
            return {
                success: false,
                error: `HTTP ${response.status}: ${errorText}`
            };
        }

    } catch (error) {
        return {
            success: false,
            error: error.message
        };
    }
};

// Delete a single file from the server
window.deleteSingleFile = async (fileId) => {
    try {
        const authToken = window.getAuthToken();
        if (!authToken) {
            throw new Error('Помилка авторизації');
        }

        const baseUrl = window.getApiBaseUrl();
        const apiUrl = `${baseUrl}/api/files/${fileId}`;

        const response = await fetch(apiUrl, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        if (response.ok) {
            return { success: true };
        } else {
            const errorText = await response.text();
            return {
                success: false,
                error: `HTTP ${response.status}: ${errorText}`
            };
        }

    } catch (error) {
        return {
            success: false,
            error: error.message
        };
    }
};

// ============================================================================
// EXISTING FUNCTIONS (unchanged)
// ============================================================================

// Real drag and drop with direct file upload
window.initializeDragAndDrop = (element, dotNetObjectRef) => {
    if (!element || !dotNetObjectRef) {
        console.error('Element or DotNet reference not provided for drag and drop initialization');
        return false;
    }

    let dragCounter = 0;

    const handleDragEnter = (e) => {
        e.preventDefault();
        e.stopPropagation();
        dragCounter++;
        
        // Only show drag state if files are being dragged
        if (e.dataTransfer.types && e.dataTransfer.types.includes('Files')) {
            if (dragCounter === 1) {
                dotNetObjectRef.invokeMethodAsync('OnDragEnter');
            }
        }
    };

    const handleDragOver = (e) => {
        e.preventDefault();
        e.stopPropagation();
        
        // Only allow drop if files are being dragged
        if (e.dataTransfer.types && e.dataTransfer.types.includes('Files')) {
            e.dataTransfer.dropEffect = 'copy';
        } else {
            e.dataTransfer.dropEffect = 'none';
        }
    };

    const handleDragLeave = (e) => {
        e.preventDefault();
        e.stopPropagation();
        dragCounter--;
        
        if (dragCounter === 0) {
            dotNetObjectRef.invokeMethodAsync('OnDragLeave');
        }
    };

    const handleDrop = async (e) => {
        e.preventDefault();
        e.stopPropagation();
        dragCounter = 0;
        
        // First hide the drag overlay
        dotNetObjectRef.invokeMethodAsync('OnDragLeave');
        
        const files = Array.from(e.dataTransfer.files);
        if (files.length === 0) return;
        
        try {
            // Validate files first
            const validationErrors = [];
            const validFiles = [];
            
            for (const file of files) {
                const validation = window.validateFile(file);
                if (validation.isValid) {
                    validFiles.push(file);
                } else {
                    validationErrors.push(validation.error);
                }
            }
            
            // Show validation errors if any
            if (validationErrors.length > 0) {
                dotNetObjectRef.invokeMethodAsync('ShowValidationErrors', validationErrors);
            }
            
            // Upload valid files
            if (validFiles.length > 0) {
                await uploadFiles(validFiles, dotNetObjectRef);
            }
            
        } catch (error) {
            console.error('Error handling dropped files:', error);
            dotNetObjectRef.invokeMethodAsync('ShowUploadError', `Помилка обробки файлів: ${error.message}`);
        }
    };

    // Add event listeners
    element.addEventListener('dragenter', handleDragEnter);
    element.addEventListener('dragover', handleDragOver);
    element.addEventListener('dragleave', handleDragLeave);
    element.addEventListener('drop', handleDrop);

    // Return cleanup function
    return () => {
        element.removeEventListener('dragenter', handleDragEnter);
        element.removeEventListener('dragover', handleDragOver);
        element.removeEventListener('dragleave', handleDragLeave);
        element.removeEventListener('drop', handleDrop);
    };
};

    // Upload files directly to API
    const uploadFiles = async (files, dotNetObjectRef) => {
        const authToken = window.getAuthToken();
        
        if (!authToken) {
            dotNetObjectRef.invokeMethodAsync('ShowUploadError', 'Помилка авторизації. Увійдіть в систему.');
            return;
        }
        
        // Show progress notification
        if (files.length > 1) {
            dotNetObjectRef.invokeMethodAsync('ShowUploadProgress', `Завантаження ${files.length} файлів...`);
        }
        
        const uploadResults = [];
        let successCount = 0;
        let errorCount = 0;
        
        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            
            try {
                // Show individual file progress for multiple files
                if (files.length > 1) {
                    dotNetObjectRef.invokeMethodAsync('ShowUploadProgress', `Завантаження ${file.name} (${i + 1}/${files.length})...`);
                } else {
                    dotNetObjectRef.invokeMethodAsync('ShowUploadProgress', `Завантаження ${file.name}...`);
                }
                
                const formData = new FormData();
                formData.append('File', file);
                
                // Use helper function to get correct API URL
                const baseUrl = window.getApiBaseUrl();
                const apiUrl = `${baseUrl}/api/files/upload`;
                
                const response = await fetch(apiUrl, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: formData
                });
                
                if (response.ok) {
                    const result = await response.json();
                    
                    if (result.success) {
                        successCount++;
                        uploadResults.push({
                            fileName: file.name,
                            success: true,
                            fileData: result.data
                        });
                    } else {
                        errorCount++;
                        uploadResults.push({
                            fileName: file.name,
                            success: false,
                            error: result.message || 'Невідома помилка'
                        });
                    }
                } else {
                    errorCount++;
                    
                    let errorMessage = `HTTP ${response.status}`;
                    
                    try {
                        const responseText = await response.text();
                        
                        if (responseText) {
                            try {
                                const errorData = JSON.parse(responseText);
                                errorMessage = errorData.error?.message || errorData.message || errorMessage;
                            } catch (parseError) {
                                errorMessage = responseText || errorMessage;
                            }
                        }
                    } catch (readError) {
                        // Ignore read errors
                    }
                    
                    if (response.status === 401) {
                        errorMessage = 'Помилка авторизації. Токен недійсний або застарів. Увійдіть в систему знову.';
                    }
                    
                    uploadResults.push({
                        fileName: file.name,
                        success: false,
                        error: errorMessage
                    });
                }
                
            } catch (error) {
                errorCount++;
                uploadResults.push({
                    fileName: file.name,
                    success: false,
                    error: error.message
                });
            }
        }
        
        // Report results to C#
        dotNetObjectRef.invokeMethodAsync('OnUploadCompleted', {
            totalFiles: files.length,
            successCount: successCount,
            errorCount: errorCount,
            results: uploadResults
        });
    };

    // Helper function to trigger file input click
    window.triggerFileUpload = (inputId) => {
        const input = document.getElementById(inputId);
        if (input) {
            input.click();
            return true;
        }
        return false;
    };
// Notification Manager для управління позиціонуванням
class NotificationManager {
    constructor() {
        this.notifications = [];
        this.container = null;
        this.initContainer();
    }

    initContainer() {
        // Створюємо контейнер для повідомлень якщо його немає
        this.container = document.getElementById('notifications-container');
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.id = 'notifications-container';
            this.container.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                z-index: 10000;
                pointer-events: none;
                max-width: 400px;
            `;
            document.body.appendChild(this.container);
        }
    }

    show(message, type = 'info', duration = 4000) {
        const notification = this.createNotification(message, type);
        
        // Додаємо в контейнер та список
        this.container.appendChild(notification);
        this.notifications.push(notification);
        
        // Анімація появи
        requestAnimationFrame(() => {
            notification.style.transform = 'translateX(0)';
            notification.style.opacity = '1';
        });
        
        // Автоматичне видалення
        setTimeout(() => {
            this.remove(notification);
        }, duration);
        
        return notification;
    }

    createNotification(message, type) {
        const notification = document.createElement('div');
        notification.className = `gaming-notification ${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <div class="notification-icon">${getNotificationIcon(type)}</div>
                <span class="notification-text">${message}</span>
                <button class="notification-close" onclick="notificationManager.remove(this.parentElement.parentElement)">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12 19 6.41z"/>
                    </svg>
                </button>
            </div>
        `;
        
        // Стилі повідомлення
        Object.assign(notification.style, {
            display: 'block',
            marginBottom: '12px',
            padding: '12px 16px',
            backgroundColor: getNotificationColor(type),
            color: 'white',
            borderRadius: '8px',
            fontFamily: 'Orbitron, monospace',
            fontWeight: 'bold',
            fontSize: '14px',
            boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
            transform: 'translateX(100%)',
            opacity: '0',
            transition: 'all 0.3s ease',
            clipPath: 'polygon(8px 0%, 100% 0%, 100% calc(100% - 8px), calc(100% - 8px) 100%, 0% 100%, 0% 8px)',
            minWidth: '250px',
            maxWidth: '100%',
            pointerEvents: 'auto',
            position: 'relative'
        });

        // Стилі для контенту
        const content = notification.querySelector('.notification-content');
        content.style.cssText = `
            display: flex;
            align-items: center;
            gap: 8px;
        `;

        // Стилі для кнопки закриття
        const closeBtn = notification.querySelector('.notification-close');
        closeBtn.style.cssText = `
            background: none;
            border: none;
            color: white;
            cursor: pointer;
            padding: 4px;
            border-radius: 4px;
            display: flex;
            align-items: center;
            justify-content: center;
            margin-left: auto;
            opacity: 0.7;
            transition: opacity 0.2s ease;
        `;

        // Hover ефект для кнопки закриття
        closeBtn.addEventListener('mouseenter', () => {
            closeBtn.style.opacity = '1';
            closeBtn.style.backgroundColor = 'rgba(255,255,255,0.1)';
        });
        closeBtn.addEventListener('mouseleave', () => {
            closeBtn.style.opacity = '0.7';
            closeBtn.style.backgroundColor = 'transparent';
        });

        return notification;
    }

    remove(notification) {
        if (!notification || !notification.parentNode) return;
        
        // Анімація зникнення
        notification.style.transform = 'translateX(100%)';
        notification.style.opacity = '0';
        
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
            // Видаляємо зі списку
            const index = this.notifications.indexOf(notification);
            if (index > -1) {
                this.notifications.splice(index, 1);
            }
        }, 300);
    }

    clear() {
        this.notifications.forEach(notification => {
            this.remove(notification);
        });
    }
}

// Створюємо глобальний менеджер повідомлень
window.notificationManager = new NotificationManager();

// Оновлена функція showNotification
window.showNotification = (message, type = 'info', duration = 4000) => {
    // Перевіряємо чи немає дубльованих повідомлень
    const existingNotifications = window.notificationManager.notifications;
    const isDuplicate = existingNotifications.some(notification => {
        const textElement = notification.querySelector('.notification-text');
        return textElement && textElement.textContent === message;
    });

    // Якщо повідомлення вже існує, не додаємо нове
    if (isDuplicate) {
        return;
    }

    return window.notificationManager.show(message, type, duration);
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

// ============================================================================
// CUSTOM SYNC CONFIRMATION DIALOG
// ============================================================================

window.showSyncConfirmDialog = (summaryJson) => {
    return new Promise((resolve) => {
        const summary = JSON.parse(summaryJson);
        
        // Text strings with proper encoding
        const texts = {
            title: '\u0421\u0438\u043d\u0445\u0440\u043e\u043d\u0456\u0437\u0430\u0446\u0456\u044f', // Синхронізація
            newFiles: '\u0414\u043e\u0434\u0430\u0442\u0438 \u043d\u043e\u0432\u0438\u0445:', // Додати нових:
            replaceFiles: '\u0417\u0430\u043c\u0456\u043d\u0438\u0442\u0438:', // Замінити:
            noChanges: '\u0411\u0435\u0437 \u0437\u043c\u0456\u043d:', // Без змін:
            cancel: '\u0421\u043a\u0430\u0441\u0443\u0432\u0430\u0442\u0438', // Скасувати
            confirm: '\u0421\u0438\u043d\u0445\u0440\u043e\u043d\u0456\u0437\u0443\u0432\u0430\u0442\u0438' // Синхронізувати
        };
        
        // Create backdrop
        const backdrop = document.createElement('div');
        backdrop.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.7);
            backdrop-filter: blur(4px);
            z-index: 10001;
            display: flex;
            align-items: center;
            justify-content: center;
            opacity: 0;
            transition: opacity 0.3s ease;
        `;
        
        // Create dialog
        const dialog = document.createElement('div');
        dialog.style.cssText = `
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
            border: 2px solid #8b5cf6;
            border-radius: 16px;
            padding: 32px;
            max-width: 500px;
            width: 90%;
            box-shadow: 0 20px 60px rgba(139, 92, 246, 0.3);
            transform: scale(0.9);
            transition: transform 0.3s ease;
            font-family: 'Orbitron', monospace;
        `;
        
        // Create title section
        const titleSection = document.createElement('div');
        titleSection.style.cssText = 'text-align: center; margin-bottom: 24px;';
        titleSection.innerHTML = `
            <svg width="64" height="64" viewBox="0 0 24 24" style="margin-bottom: 16px;">
                <path fill="#8b5cf6" d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
            </svg>
            <h2 style="color: #8b5cf6; margin: 0; font-size: 24px; font-weight: bold;"></h2>
        `;
        titleSection.querySelector('h2').textContent = texts.title;
        
        // Create content section
        const contentSection = document.createElement('div');
        contentSection.style.cssText = 'background: rgba(139, 92, 246, 0.1); border-radius: 12px; padding: 20px; margin-bottom: 24px;';
        
        const statsGrid = document.createElement('div');
        statsGrid.style.cssText = 'display: grid; gap: 12px;';
        
        // Add new files row if needed
        if (summary.NewFiles > 0) {
            const newFilesRow = document.createElement('div');
            newFilesRow.style.cssText = 'display: flex; align-items: center; gap: 12px; color: #10B981;';
            newFilesRow.innerHTML = `
                <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
                </svg>
                <span style="font-weight: 600;"></span>
                <span style="margin-left: auto; font-size: 20px;">${summary.NewFiles}</span>
            `;
            newFilesRow.querySelector('span').textContent = texts.newFiles;
            statsGrid.appendChild(newFilesRow);
        }
        
        // Add replaced files row if needed
        if (summary.ReplacedFiles > 0) {
            const replacedFilesRow = document.createElement('div');
            replacedFilesRow.style.cssText = 'display: flex; align-items: center; gap: 12px; color: #F59E0B;';
            replacedFilesRow.innerHTML = `
                <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M12 6v3l4-4-4-4v3c-4.42 0-8 3.58-8 8 0 1.57.46 3.03 1.24 4.26L6.7 14.8c-.45-.83-.7-1.79-.7-2.8 0-3.31 2.69-6 6-6zm6.76 1.74L17.3 9.2c.44.84.7 1.79.7 2.8 0 3.31-2.69 6-6 6v-3l-4 4 4 4v-3c4.42 0 8-3.58 8-8 0-1.57-.46-3.03-1.24-4.26z"/>
                </svg>
                <span style="font-weight: 600;"></span>
                <span style="margin-left: auto; font-size: 20px;">${summary.ReplacedFiles}</span>
            `;
            replacedFilesRow.querySelector('span').textContent = texts.replaceFiles;
            statsGrid.appendChild(replacedFilesRow);
        }
        
        // Add unchanged files row
        const unchangedRow = document.createElement('div');
        unchangedRow.style.cssText = 'display: flex; align-items: center; gap: 12px; color: #8b5cf6;';
        unchangedRow.innerHTML = `
            <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41L9 16.17z"/>
            </svg>
            <span style="font-weight: 600;"></span>
            <span style="margin-left: auto; fontSize: 20px;">${summary.UnchangedFiles}</span>
        `;
        unchangedRow.querySelector('span').textContent = texts.noChanges;
        statsGrid.appendChild(unchangedRow);
        
        contentSection.appendChild(statsGrid);
        
        // Create buttons section
        const buttonsSection = document.createElement('div');
        buttonsSection.style.cssText = 'display: flex; gap: 12px;';
        
        // Cancel button
        const cancelBtn = document.createElement('button');
        cancelBtn.id = 'sync-cancel-btn';
        cancelBtn.textContent = texts.cancel;
        cancelBtn.style.cssText = `
            flex: 1;
            padding: 14px 24px;
            background: rgba(239, 68, 68, 0.1);
            border: 2px solid #EF4444;
            color: #EF4444;
            border-radius: 8px;
            font-family: 'Orbitron', monospace;
            font-weight: bold;
            font-size: 14px;
            cursor: pointer;
            transition: all 0.3s ease;
        `;
        cancelBtn.onmouseover = () => {
            cancelBtn.style.background = '#EF4444';
            cancelBtn.style.color = 'white';
        };
        cancelBtn.onmouseout = () => {
            cancelBtn.style.background = 'rgba(239, 68, 68, 0.1)';
            cancelBtn.style.color = '#EF4444';
        };
        
        // Confirm button
        const confirmBtn = document.createElement('button');
        confirmBtn.id = 'sync-confirm-btn';
        confirmBtn.textContent = texts.confirm;
        confirmBtn.style.cssText = `
            flex: 1;
            padding: 14px 24px;
            background: linear-gradient(135deg, #8b5cf6 0%, #6d28d9 100%);
            border: none;
            color: white;
            border-radius: 8px;
            font-family: 'Orbitron', monospace;
            font-weight: bold;
            font-size: 14px;
            cursor: pointer;
            box-shadow: 0 4px 12px rgba(139, 92, 246, 0.4);
            transition: all 0.3s ease;
        `;
        confirmBtn.onmouseover = () => {
            confirmBtn.style.transform = 'translateY(-2px)';
            confirmBtn.style.boxShadow = '0 6px 16px rgba(139, 92, 246, 0.6)';
        };
        confirmBtn.onmouseout = () => {
            confirmBtn.style.transform = 'translateY(0)';
            confirmBtn.style.boxShadow = '0 4px 12px rgba(139, 92, 246, 0.4)';
        };
        
        buttonsSection.appendChild(cancelBtn);
        buttonsSection.appendChild(confirmBtn);
        
        // Assemble dialog
        dialog.appendChild(titleSection);
        dialog.appendChild(contentSection);
        dialog.appendChild(buttonsSection);
        backdrop.appendChild(dialog);
        document.body.appendChild(backdrop);
        
        // Animate in
        requestAnimationFrame(() => {
            backdrop.style.opacity = '1';
            dialog.style.transform = 'scale(1)';
        });
        
        // Handle buttons
        const close = (result) => {
            backdrop.style.opacity = '0';
            dialog.style.transform = 'scale(0.9)';
            setTimeout(() => {
                document.body.removeChild(backdrop);
                resolve(result);
            }, 300);
        };
        
        cancelBtn.onclick = () => close(false);
        confirmBtn.onclick = () => close(true);
        backdrop.onclick = (e) => {
            if (e.target === backdrop) close(false);
        };
    });
};