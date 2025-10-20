// SharpGraph IDE JavaScript Functions

$(document).ready(function() {
    // Initialize AdminLTE components
    if (typeof AdminLTE !== 'undefined') {
        AdminLTE.init();
    }
    
    // Auto-refresh database status
    refreshDatabaseStatus();
    setInterval(refreshDatabaseStatus, 30000); // Refresh every 30 seconds
    
    // Initialize tooltips
    $('[data-toggle="tooltip"]').tooltip();
    
    // GraphQL query editor enhancements
    initializeGraphQLEditor();
    
    // Performance metrics auto-refresh
    if (window.location.pathname === '/metrics' || window.location.pathname === '/') {
        setInterval(refreshMetrics, 5000); // Refresh every 5 seconds
    }
});

// Database status refresh
function refreshDatabaseStatus() {
    // This would be connected to a Blazor component or API
    // For now, just update the UI elements
    const statusBadge = $('.navbar-badge');
    const statusIcon = $('.fa-database');
    
    // Simulate status check (in real implementation, this would be an API call)
    const isOnline = true; // This would come from actual status check
    
    if (isOnline) {
        statusBadge.removeClass('badge-danger badge-warning').addClass('badge-success').text('Online');
        statusIcon.removeClass('text-danger text-warning').addClass('text-success');
    } else {
        statusBadge.removeClass('badge-success badge-warning').addClass('badge-danger').text('Offline');
        statusIcon.removeClass('text-success text-warning').addClass('text-danger');
    }
}

// GraphQL editor enhancements
function initializeGraphQLEditor() {
    const editor = $('.graphql-editor');
    if (editor.length > 0) {
        // Add basic syntax highlighting classes
        editor.on('input', function() {
            highlightGraphQLSyntax($(this));
        });
        
        // Add Ctrl+Enter shortcut to execute query
        editor.on('keydown', function(e) {
            if ((e.ctrlKey || e.metaKey) && e.keyCode === 13) {
                e.preventDefault();
                executeGraphQLQuery();
            }
        });
    }
}

// Basic GraphQL syntax highlighting
function highlightGraphQLSyntax(element) {
    // This is a simplified version - in production you'd use a proper syntax highlighter
    let content = element.text();
    
    // Highlight keywords
    content = content.replace(/\b(query|mutation|subscription|type|interface|union|enum|input|fragment|schema|extend)\b/g, 
        '<span class="text-primary font-weight-bold">$1</span>');
    
    // Highlight field names
    content = content.replace(/(\w+)(\s*:)/g, '<span class="text-info">$1</span>$2');
    
    // Note: This is a basic example - real implementation would use CodeMirror or Monaco
}

// Execute GraphQL query
function executeGraphQLQuery() {
    const queryText = $('.graphql-editor').val();
    if (!queryText.trim()) {
        showAlert('Please enter a GraphQL query', 'warning');
        return;
    }
    
    // Show loading state
    $('.graphql-results').html('<div class="text-center p-4"><div class="loading-spinner"></div> Executing query...</div>');
    
    // This would be connected to a Blazor component method
    // For now, just simulate the execution
    setTimeout(() => {
        const mockResult = {
            data: {
                message: "Query executed successfully",
                timestamp: new Date().toISOString()
            }
        };
        
        $('.graphql-results').html('<pre>' + JSON.stringify(mockResult, null, 2) + '</pre>');
    }, 1000);
}

// Refresh performance metrics
function refreshMetrics() {
    // This would call Blazor component methods to refresh data
    // For now, just add a visual indicator
    $('.metric-card').each(function() {
        $(this).addClass('pulse-animation');
        setTimeout(() => {
            $(this).removeClass('pulse-animation');
        }, 300);
    });
}

// Show alert notifications
function showAlert(message, type = 'info') {
    const alertClass = `alert-${type}`;
    const alert = $(`
        <div class="alert ${alertClass} alert-dismissible fade show" role="alert" style="position: fixed; top: 20px; right: 20px; z-index: 9999; min-width: 300px;">
            ${message}
            <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                <span aria-hidden="true">&times;</span>
            </button>
        </div>
    `);
    
    $('body').append(alert);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        alert.alert('close');
    }, 5000);
}

// Format JSON for display
function formatJSON(obj) {
    return JSON.stringify(obj, null, 2);
}

// Copy text to clipboard
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        showAlert('Copied to clipboard!', 'success');
    }).catch(() => {
        showAlert('Failed to copy to clipboard', 'error');
    });
}

// Export data as JSON
function exportAsJSON(data, filename = 'sharpgraph-export.json') {
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

// Format file size
function formatFileSize(bytes) {
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    if (bytes === 0) return '0 Bytes';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
}

// Format numbers with commas
function formatNumber(num) {
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

// Pulse animation CSS
const style = document.createElement('style');
style.textContent = `
    .pulse-animation {
        animation: pulse 0.3s ease-in-out;
    }
    
    @keyframes pulse {
        0% { transform: scale(1); }
        50% { transform: scale(1.02); }
        100% { transform: scale(1); }
    }
`;
document.head.appendChild(style);