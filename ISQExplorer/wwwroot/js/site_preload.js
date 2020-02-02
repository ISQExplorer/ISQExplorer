// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/**
 * Returns all of the elements matching the given query string.
 * @param {string} id
 * @returns {NodeListOf<HTMLElementTagNameMap[*]>}
 */
const queryAll = id => document.querySelectorAll(id);

/**
 * Returns the first element matching the given query string.
 * @param {string} id
 * @returns {HTMLElement}
 */
const query = id => {
    const elem = document.querySelector(id);
    if (elem == null) {
        throw `No element found matching selector '${id}';`
    }
    return elem;
};

/**
 * Returns a query string from an object.
 * @returns {string}
 * @param {object} obj
 */
const queryString = obj => {
    if (obj.keys.length === 0) {
        return "";
    }
    
    return "/" + obj.keys.map(key => `${key}=${obj[key]}`).join("&");
};

/**
 * Merges two objects into one.
 * @param {object} obj1
 * @param {object} obj2
 * @returns {object}
 */
const merge = (obj1, obj2) => Object.assign(Object.assign({}, obj1), obj2);
