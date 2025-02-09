﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// WARNING: The implementation of functions in this class are subject to change and the implementation details should not be relied on to provide a stable method of obtaining the required information

var PowerAppsPortalConnections = class {
    static getConnections() {
        var dom = document.getElementsByClassName('connections-list-container');

        if (dom.length == 0) {
            return "[]";
        }

        dom = dom[0]

        const key = Object.keys(dom).find(key => {
            return key.startsWith("__reactFiber$") // react 17+
                || key.startsWith("__reactInternalInstance$"); // react <17
        });
        const domFiber = dom[key];
        if (domFiber == null) return null;

        // Assume version of react is 16+
        const GetCompFiber = fiber => {
            let parentFiber = fiber.return;
            while (typeof parentFiber.type == "string") {
                parentFiber = parentFiber.return;
            }
            return parentFiber;
        };
        let compFiber = GetCompFiber(domFiber);

        // Keep moving up until find the required list
        while (typeof compFiber?.stateNode?.props?.connectionsListItems === 'undefined') {
            compFiber = GetCompFiber(compFiber.return)
        }
        var items = compFiber.stateNode.props.connectionsListItems;

        var connections = []

        items.forEach(item => {
            connections.push({ name: item.api.name, id: item.connection.name, status: item.statusMessage });
        })

        return JSON.stringify(connections)

    }

    static getInstanceUrl() {
        var dom = document.getElementsByClassName('ms-Dialog-content');

        if (dom.length == 0) {
            return "";
        }

        dom = dom[0]

        const key = Object.keys(dom).find(key => {
            return key.startsWith("__reactFiber$") // react 17+
                || key.startsWith("__reactInternalInstance$"); // react <17
        });
        const domFiber = dom[key];
        if (domFiber == null) return null;

        var match = null


        var current = dom[key]
        
        // Search for content item of dialog
        while (match == null && current.child != null) {
            if (typeof current.memoizedProps.className === 'string' && current.memoizedProps.className.indexOf('content-') >= 0) {
                match = current;
            } else {
                current = current.child;
            }
        }

        if (match != null) {
            var url = "";
            // Search for the instance url
            match.memoizedProps.children.forEach(item => {
                // TODO Handle Localization
                if (item.props.children[0].trim().toLowerCase() == 'instance url:') {
                    url = item.props.children[1]
                }
            })
        }

        return url;
    }
}
