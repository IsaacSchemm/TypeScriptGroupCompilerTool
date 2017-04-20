function page2Start(): string {
    return `${onlyOnPage2()} - You are on page ${getPageId()} at time ${sharedFunction()} and the type of getPageId() is ${typeof getPageId()}`;
}