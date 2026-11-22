 UPDATE product_presentations AS presentation
    SET color_id = color.id
    FROM colors AS color
    WHERE presentation.color_id IS NULL
        AND (
        lower(translate(btrim(presentation.name), 'áéíóúüñÁÉÍÓÚÜÑ', 'aeiouunAEIOUUN')) =
            lower(translate(color.name, 'áéíóúüñÁÉÍÓÚÜÑ', 'aeiouunAEIOUUN'))
        OR (lower(btrim(presentation.name)) = 'beis' AND color.name = 'Beige')
        );