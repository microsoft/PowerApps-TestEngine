class PowerAppsPortal {
    static idleStatus() {
        var element = document.getElementById('O365_MainLink_Settings');
        if (typeof (element) != 'undefined' && element != null) {
            return 'Idle'
        } else {
            return 'Loading'
        }
    }

    static sessionDetails() {
        return waitForElelement('O365_MainLink_Settings')
            .then(element => {
                element.click();

                waitForElelement('sessionDetails-help-menu-item').then(
                    details => {
                        details.click()
                    }
                )
            })
    }

    static waitForElelement(selector) {
        return new Promise(resolve => {
            if (document.querySelector(selector)) {
                return resolve(document.querySelector(selector));
            }

            const observer = new MutationObserver(mutations => {
                if (document.querySelector(selector)) {
                    observer.disconnect();
                    resolve(document.querySelector(selector));
                }
            });

            observer.observe(document.body, {
                childList: true,
                subtree: true
            });
        });
    }
}