// Example rating ranges for traps
const trapDict = {
    'fools_mate': { 'name': 'Fools Mate', '1600': 2.291077917, '1800': 1.025451863, '2000': 0.0, '2200': 0.0, '2500': 0.3714710253 },
    'scholars_mate': { 'name': 'Scholars Mate', '1600': 0.9674549633, '1800': 0.4297590531, '2000': 0.2203375738, '2200': 0.1656543346, '2500': 0.09543077452 },
    'lasker_trap': { 'name': 'Lasker Trap', '1600': 15.33295861, '1800': 10.64861842, '2000': 5.255073446, '2200': 2.45424048, '2500': 1.696402457 },
    'budapest_trap': { 'name': 'Budapest Trap', '1600': 3.703703704, '1800': 3.431372549, '2000': 3.529411765, '2200': 1.538461538, '2500': 22.22222222 },
    'halosar_trap': { 'name': 'Halosar Trap', '1600': 27.14107366, '1800': 26.69950739, '2000': 26.32862019, '2200': 32.91925466, '2500': 50.0 },
    'monticelli_trap': { 'name': 'Monticelli Trap', '1600': 1.224177506, '1800': 0.4958677686, '2000': 0.1779007709, '2200': 0.2340354397, '2500': 0.0 },
    'kieninger_trap': { 'name': 'Kieninger Trap', '1600': 53.6643026, '1800': 35.43981481, '2000': 14.60281831, '2200': 3.99113082, '2500': 0.0 },
    'blackburne_shilling_gambit': { 'name': 'Blackburne Shilling Gambit', '1600': 24.69795132, '1800': 17.34219063, '2000': 15.48062549, '2200': 15.82655827, '2500': 28.94736842 },
    'marshall_trap': { 'name': 'Marshall Trap', '1600': 6.666666667, '1800': 8.163265306, '2000': 4.47761194, '2200': 2.272727273, '2500': 0.0 },
    'elephant_trap': { 'name': 'Elephant Trap', '1600': 33.75540457, '1800': 16.47100676, '2000': 5.656898513, '2200': 1.202614379, '2500': 0.1385041551 },
    'mortimer_trap': { 'name': 'Mortimer Trap', '1600': 43.6123348, '1800': 42.01680672, '2000': 32.19102366, '2200': 23.03961196, '2500': 8.30449827 },
    'tarrasch_trap': { 'name': 'Tarrasch Trap', '1600': 0.0, '1800': 0.0, '2000': 3.773584906, '2200': 7.142857143, '2500': 0.0 },
    'magnus_smith_trap': { 'name': 'Magnus Smith Trap', '1600': 29.56696728, '1800': 29.65871536, '2000': 20.89588788, '2200': 9.114871461, '2500': 3.908396947 },
    'wurzburger_trap': { 'name': 'Würzburger Trap', '1600': 7.916437603, '1800': 16.71317233, '2000': 21.81210702, '2200': 19.88832757, '2500': 10.33125615 }
};
const result = document.getElementById('result');
const trapName = document.getElementById('trap-name');

function closest(needle, haystack) {
    return haystack.reduce((a, b) => {
        let aDiff = Math.abs(a - needle);
        let bDiff = Math.abs(b - needle);

        if (aDiff == bDiff) {
            return a > b ? a : b;
        } else {
            return bDiff < aDiff ? b : a;
        }
    });
}

function chooseTrap() {
    let possible = [1600, 1800, 2000, 2200, 2500];
    let rating = Number(document.getElementById('rating').value);

    let target = closest(rating, possible)


    if (Number.isInteger(rating) && rating >= 0) {
        var trap;
        var trapObj;
        var folder = 0;
        let trapImageName;

        // Iterate over traps, identifying which trap rating falls under
        for (let i = 0; i < Object.keys(trapDict).length; i++) {
            let currentTrap = trapDict[Object.keys(trapDict)[i]];

            let successRate = currentTrap[target];
            if (successRate > folder) {
                folder = successRate
                trapObj = currentTrap
                trapImageName = Object.keys(trapDict)[i]
            }
        }

        trap = trapObj["name"]

        if (trap != undefined) {
            trapName.setAttribute('data-caption', trap);
            trapName.href = 'img/traps/' + trapImageName + '.png';
            UIkit.lightbox(result).show();
        }
        else { // Defualts to Würzburger Trap if rating is not in range
            trapName.setAttribute('data-caption', 'Würzburger Trap');
            trapName.href = 'img/traps/wurzburger_trap.png';
            UIkit.lightbox(result).show();
        }
    } else {
        UIkit.notification({
            message: 'Rating must be a positive number!', pos: 'top-center', status: 'danger'
        });
    }
}



var children = document.querySelectorAll(".moves > tbody:nth-child(2) > tr > td:nth-child(2)");
var summer = 0;
for (var i = 0; i < children.length; i++) {
    var tableChild = children[i];
    summer = summer + parseInt(tableChild.innerText.replace(",", ""));
}