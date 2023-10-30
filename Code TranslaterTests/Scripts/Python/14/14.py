def create_card():

    print('Placeholder')

    tab_width = root.winfo_width()
    tab_height = root.winfo_height()


    print(tab_width, tab_height)
    tab = ttk.Frame(notebook, width=tab_width, height=tab_height)
    tab.pack(fill="both", expand=1)
    
    notebook.add(tab, text="Add Card")
    notebook.select(tab)


    card_Name = Entry(tab, name="cardName")
    card_Name.pack()

    Frame1 = Frame(tab, width=tab_width)
    Frame1.pack()
    Frame2 = Frame(tab, width=tab_width)

    values = {
            "Frame1" : ("Green",
                        "Yellow",
                        "Red"),

            "Frame2" : {"TButton1" : "Vegetarian",
                        "TButton2" : "Vegan",
                        "TButton3" : "Pescotarian",}
            }
    
    strV = StringVar()
    
    for cListE in values["Frame1"]:
        radioButton = Radiobutton(Frame1, text = cListE, value = cListE, variable = strV)
        radioButton.pack()
        state = radioButton["state"]
