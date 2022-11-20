 < !--Google Charts-- >
            <script type="text/javascript" src="https://www.google.com/jsapi"></script>

            <script type="text/javascript">
                google.load("visualization", "1", { packages: ["corechart"] });
                google.setOnLoadCallback(drawChart);
                function drawChart() {
                    $.ajax({
                        type: "POST",
                        url: "/Home/GglProjectPriority",
                        data: '{}',
                        contentType: "application/json; charset-utf-8",
                        dataType: "json",
                        success: function (result) {
                            var data = google.visualization.arrayToDataTable(result);

                            //3D Pie
                            var options = {
                                title: 'Project Priority',
                                is3D: true,
                                chartArea: { left: 0, bottom: 15, width: "100%", height: "100%" },
                                legend: { position: 'bottom' }
                            };

                            var chart = new google.visualization.PieChart($("#chart3D")[0]);
                            chart.draw(data, options);
                        },
                        failure: function (rresult) {
                            alert(result.d);
                        },
                        error: function (result) {
                            alert(result.d);
                        },
                    })
                }
            </script>
            <script type="text/javascript">
                google.load("visualization", "1", { packages: ["corechart"] });
                google.setOnLoadCallback(drawChart);
                function drawChart() {
                    $.ajax({
                        type: "POST",
                        url: "/Home/GglProjectTickets",
                        data: '{}',
                        contentType: "application/json; charset-utf-8",
                        dataType: "json",
                        success: function (result) {
                            var data = google.visualization.arrayToDataTable(result);

                            //Donut
                            var options = {
                                title: 'Company Ticket Distribution',
                                pieHole: 0.3,
                                chartArea: { left: 0, bottom: 15, width: "100%", height: "100%" },
                                legend: { position: 'bottom' }
                            };

                            var chart = new google.visualization.PieChart($("#donut")[0]);
                            chart.draw(data, options);
                        },
                        failure: function (rresult) {
                            alert(result.d);
                        },
                        error: function (result) {
                            alert(result.d);
                        },
                    })
                }
            </script>

@* Morris Donut Chart *@
            <script src="//cdnjs.cloudflare.com/ajax/libs/raphael/2.1.0/raphael-min.js"></script>
            <script src="//cdnjs.cloudflare.com/ajax/libs/morris.js/0.5.1/morris.min.js"></script>
            <script>

	var morrisDonutData = [{
		label: "Low",
		value: @Model.Tickets.Where(t=>t.TicketPriority.Name == nameof(BTTicketPriority.Low)).Count()
	}, {
		label: "Medium",
		value: @Model.Tickets.Where(t=>t.TicketPriority.Name == nameof(BTTicketPriority.Medium)).Count()
	}, {
		label: "High",
		value: @Model.Tickets.Where(t=>t.TicketPriority.Name == nameof(BTTicketPriority.High)).Count()
	}, {
		label: "Urgent",
		value: @Model.Tickets.Where(t=>t.TicketPriority.Name == nameof(BTTicketPriority.Urgent)).Count()
	}];


	/*
	Morris: Donut
	*/
	if ($('#morrisTicketPriority').get(0)) {
		var donutChart = Morris.Donut({
			resize: true,
			element: 'morrisTicketPriority',
			data: morrisDonutData,
			colors: ['#0088cc', '#734ba9', '#E36159', '#ff993b']
		});

		donutChart.options.data.forEach(function(label, i) {
			var legendItem = $('<span></span>').text( label['label'] + ": " +label['value']).prepend('<span>&nbsp;</span>');
			legendItem.find('span')
			  .css('backgroundColor', donutChart.options.colors[i])
			  .css('width', '20px')
			  .css('display', 'inline-block')
			  .css('margin', '10px');
			$('#legend').append(legendItem)
		});
	};
            </script>

            <!-- *** Begin Chart JS Pie / Donut *** -->
            <script src="https://cdn.jsdelivr.net/npm/chart.js@3.6.0/dist/chart.min.js"></script>
            <script>

	var donutChartCanvas = $('#donutChart').get(0).getContext('2d');
	var donutData = {
		labels: [
			'@nameof(BTTicketStatus.Development)',
			'@nameof(BTTicketStatus.New)',
			'@nameof(BTTicketStatus.Resolved)',
			'@nameof(BTTicketStatus.Testing)'
		],
		datasets: [
			{
				data: [
					@Model.Tickets.Where(t=> t.TicketStatus.Name == nameof(BTTicketStatus.Development)).Count(),
					@Model.Tickets.Where(t=> t.TicketStatus.Name ==nameof(BTTicketStatus.New)).Count(),
					@Model.Tickets.Where(t=> t.TicketStatus.Name ==nameof(BTTicketStatus.Resolved)).Count(),
					@Model.Tickets.Where(t=> t.TicketStatus.Name ==nameof(BTTicketStatus.Testing)).Count()
				],
				backgroundColor: [
					'rgba(255, 99, 132, 0.2)',
					'rgba(54, 162, 235, 0.2)',
					'rgba(255, 206, 86, 0.2)',
					'rgba(75, 192, 192, 0.2)'
				],
				borderColor: [
					'rgba(255, 99, 132, 1)',
					'rgba(54, 162, 235, 1)',
					'rgba(255, 206, 86, 1)',
					'rgba(75, 192, 192, 1)'
				]
			}
		]
	};

	var donutOptions = {
		maintainAspectRatio: false,
		responsive: true,
	};
	//Create pie or douhnut chart
	// You can switch between pie and douhnut using the method below.
	new Chart(donutChartCanvas, {
		type: 'doughnut',
		data: donutData,
		options: donutOptions
	});
            </script>
            <!-- *** End Chart JS Pie / Donut *** -->
            <!-- *** Begin AM Charts *** -->
            < !--Resources -->
            <script src="https://cdn.amcharts.com/lib/4/core.js"></script>
            <script src="https://cdn.amcharts.com/lib/4/charts.js"></script>

            <!--Chart code-- >
            <script>
                $.ajax({
                    type: "POST",
                    url: "/Home/AmCharts",
                    data: '{}',
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (result) {
                        am4core.ready(function () {

                            // Themes begin
                            // Themes end

                            // Create chart instance
                            var chart = am4core.create("chartdiv", am4charts.XYChart);


                            // Add data
                            chart.data = result;

                            // Create axes
                            var categoryAxis = chart.yAxes.push(new am4charts.CategoryAxis());
                            categoryAxis.dataFields.category = "project";
                            categoryAxis.numberFormatter.numberFormat = "#";
                            categoryAxis.renderer.inversed = true;
                            categoryAxis.renderer.grid.template.location = 0;
                            categoryAxis.renderer.cellStartLocation = 0.1;
                            categoryAxis.renderer.cellEndLocation = 0.9;

                            var valueAxis = chart.xAxes.push(new am4charts.ValueAxis());
                            valueAxis.renderer.opposite = true;

                            // Create series
                            function createSeries(field, name) {
                                var series = chart.series.push(new am4charts.ColumnSeries());
                                series.dataFields.valueX = field;
                                series.dataFields.categoryY = "project";
                                series.name = name;
                                series.columns.template.tooltipText = "{name}: [bold]{valueX}[/]";
                                series.columns.template.height = am4core.percent(100);
                                series.sequencedInterpolation = true;

                                var valueLabel = series.bullets.push(new am4charts.LabelBullet());
                                valueLabel.label.text = "{valueX}";
                                valueLabel.label.horizontalCenter = "left";
                                valueLabel.label.dx = 10;
                                valueLabel.label.hideOversized = false;
                                valueLabel.label.truncate = false;

                                var categoryLabel = series.bullets.push(new am4charts.LabelBullet());
                                categoryLabel.label.text = "{name}";
                                categoryLabel.label.horizontalCenter = "right";
                                categoryLabel.label.dx = -10;
                                categoryLabel.label.fill = am4core.color("#fff");
                                categoryLabel.label.hideOversized = false;
                                categoryLabel.label.truncate = false;
                            }

                            createSeries("tickets", "Tickets");
                            createSeries("developers", "Devs");

                        }); // end am4core.ready()


                    },
                    failure: function (result) {
                        alert(result.d);
                    },
                    error: function (result) {
                        alert(result.d);
                    }
                });
            </script>
            <!--Plotly Charts-- >
            < !--Load plotly.js into the DOM-- >
            <script src='https://cdn.plot.ly/plotly-2.4.2.min.js'></script>
            <script>
                $.ajax({
                    type: "POST",
                    url: "/Home/PlotlyBarChart",
                    data: '{}',
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (result) {
                        var data = result;

                        var layout = { barmode: 'group' };

                        Plotly.newPlot('plotlyChart', data, layout);
                    },
                    failure: function (result) {
                        alert(result.d);
                    },
                    error: function (result) {
                        alert(result.d);
                    }
                });
            </script>

        }