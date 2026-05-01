async function authLogin(url, payload) {
    const response = await fetch(url, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(payload),
        credentials: "include"
    });

    return response.ok;
}

async function authLogout(url) {
    const response = await fetch(url, {
        method: "POST",
        credentials: "include"
    });

    return response.ok;
}
