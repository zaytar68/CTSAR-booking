window.planningHelper = {
    dotNetRef: null,
    resizeTimeout: null,

    initialize: function (dotNetReference) {
        this.dotNetRef = dotNetReference;

        // Écouter les changements de taille de fenêtre avec debounce
        window.addEventListener('resize', () => {
            if (this.resizeTimeout) {
                clearTimeout(this.resizeTimeout);
            }

            this.resizeTimeout = setTimeout(() => {
                if (this.dotNetRef) {
                    const width = window.innerWidth;
                    this.dotNetRef.invokeMethodAsync('OnWindowResize', width);
                }
            }, 250);
        });
    },

    getWindowWidth: function () {
        return window.innerWidth;
    },

    dispose: function () {
        this.dotNetRef = null;
        if (this.resizeTimeout) {
            clearTimeout(this.resizeTimeout);
        }
    }
};
