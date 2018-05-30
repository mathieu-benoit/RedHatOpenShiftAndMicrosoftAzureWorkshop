/*
* D3 Gauge Control.
* Encapsulated example from: http://bl.ocks.org/NPashaP/59c2c7483fb61070486835d15c807941
* Licence: GNU General Public License, version 3.
* Authors: Pasha, Jovan Popovic
**************************************************************************/

var GraphVizGauge = function (target, options) {

    options = options || { from: 0, to: 10 };
    this.options = options;

    var svg = d3.select(target);
    try {
        options.size = svg[0][0].clientWidth || 600;
    } catch (ex) { options.size = 600; }

    var g = svg.append("g").attr("transform", "translate(" + options.size / 2 + "," + options.size / 2 + ")");
    var domain = [options.from || 0, options.to || 10];

    var gg = viz.gg()
        .domain(domain)
        .ticks(d3.range(domain[0], domain[1] + 1, options.tick || 1))
        .outerRadius(options.outerRadius || options.size / 2)
        .innerRadius(options.innerRadius || 30)
        .value(0)
        .duration(options.duration || 1000);

    gg.defs(svg);
    g.call(gg);

    this.Data = function (data) {
        gg.setNeedle(Math.min(data, options.to));
    }
}