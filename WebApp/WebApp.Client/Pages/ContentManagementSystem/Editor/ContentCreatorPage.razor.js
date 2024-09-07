window.InitCssAutoComplete = () => handle();

function handle() {
    require(['vs/editor/editor.main'], function () {

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
                const cssClasses = extractCSSClasses(cssContent);
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