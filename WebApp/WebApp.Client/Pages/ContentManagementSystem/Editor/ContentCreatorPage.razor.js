window.InitCssAutoComplete = () => handle();

function handle() {
    require(['vs/editor/editor.main'], function () {

        // Function to load all CSS from <style> and <link> tags in <head>
        async function loadAllCSS() {
            let cssContent = '';

            // Get CSS from <style> tags
            document.querySelectorAll('style').forEach(styleTag => {
                cssContent += styleTag.innerHTML;
            });

            // Fetch CSS from <link> tags with rel="stylesheet"
            const linkPromises = Array.from(document.querySelectorAll('link[rel="stylesheet"]')).map(linkTag => {
                return fetch(linkTag.href)
                    .then(response => response.text())
                    .then(css => {
                        cssContent += css;
                    })
                    .catch(err => console.error('Error loading CSS:', err));
            });

            // Wait for all external CSS files to load
            await Promise.all(linkPromises);

            return cssContent;
        }

        // Function to extract CSS classes from the CSS editor content
        function extractCSSClasses(cssCode) {
            const regex = /\.([a-zA-Z0-9_-]+)\s*{/g;
            const classes = [];
            let match;
            while ((match = regex.exec(cssCode)) !== null) {
                classes.push(match[1]);
            }
            return classes;
        }

        // Register a completion provider for the HTML editor
        monaco.languages.registerCompletionItemProvider('html', {
            provideCompletionItems: async function () {
                let cssContent = await DotNet.invokeMethodAsync('WebApp.Client', 'GetCurrentStyle');
                const allCSSContent = extractCSSClasses(await loadAllCSS());
                const cssClasses = extractCSSClasses(cssContent);
                cssClasses.push(...allCSSContent);
  
                const suggestions = cssClasses.map(cssClass => ({
                    label: cssClass,
                    kind: monaco.languages.CompletionItemKind.Keyword,
                    insertText: cssClass,
                    range: null
                }));
                return {suggestions: suggestions};
            }
        });
    });
}

handle();