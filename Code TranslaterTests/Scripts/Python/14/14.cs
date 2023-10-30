static void create_card()
{
	
	Console.WriteLine("Placeholder");
	
	var tab_width = root.winfo_width();
	var tab_height = root.winfo_height();
	
	
	Console.WriteLine(tab_width, tab_height);
	var tab = ttk.Frame(notebook, width = tab_width, height = tab_height);
	tab.pack(fill = "both", expand = 1);
	
	notebook.add(tab, text = "Add Card");
	notebook.select(tab);
	
	
	var card_Name = Entry(tab, name = "cardName");
	card_Name.pack();
	
	var Frame1 = Frame(tab, width = tab_width);
	Frame1.pack();
	var Frame2 = Frame(tab, width = tab_width);
	
	Dictionary<object, object> values = new Dictionary<object, object>
	{
		{ "Frame1", new List<object> { "Green", "Yellow", "Red" } }, 
		{ "Frame2", new Dictionary<object, object>
			{
				{ "TButton1", "Vegetarian" }, 
				{ "TButton2", "Vegan" }, 
				{ "TButton3", "Pescotarian" }
			}
		 }
	};
	
	var strV = StringVar();
	
	foreach (var cListE in values["Frame1"])
	{
		var radioButton = Radiobutton(Frame1, text = cListE, value = cListE, variable = strV);
		radioButton.pack();
		var state = radioButton["state"];
	}
}